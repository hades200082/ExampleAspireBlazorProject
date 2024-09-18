using System.Net;
using System.Net.Mime;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Application.Host.Api;
using Application.Host.Api.Extensions;
using Application.Host.Api.Middleware;
using Application.Host.Api.RateLimiting;
using Application.Host.Api.Services;
using Application.Services;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Cysharp.Serialization.Json;
using Domain.Startup;
using Gridify;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using RedisRateLimiting;
using Serilog;
using Shared.Enums;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSerilog((services, cfg) => cfg
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.AddServiceDefaults()
        .AddAuthentication()
        .AddApplicationServices(ApplicationTypes.WebApp);

    builder.Services.AddSingleton<IRedactionService, RedactionService>();

    builder.Services.AddRouting(opt =>
    {
        opt.ConstraintMap.Add("ulid", typeof(UlidRouteConstraint));
    }).AddControllers(options =>
    {
        options.InputFormatters.Insert(0, JsonPatchInputFormatterProvider.GetInputFormatter());
        options.Conventions.Add(new RouteConvention()); // Pluralise & kebab-case
        options.Filters.Add(new ProducesAttribute(MediaTypeNames.Application.Json)); // Always JSON
    }).AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new UlidJsonConverter());
    });

    // Ensures that HttpContext.Request.RemoteIpAddress gets the correct IP address
    // when the service is hosted behind a proxy or load-balancer
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    #region Performance management

    var enableLoadShedding = builder.Configuration.GetValue("Features:EnableLoadShedding", false);
    var enableRateLimiting = builder.Configuration.GetValue("Features:EnableRateLimiting", false);

    if (enableLoadShedding)
    {
        builder.Services.AddLoadShedding((_, options) =>
        {
            options.AdaptativeLimiter.UseEndpointPriorityResolver();
            options.SubscribeEvents(events =>
            {
                events.ItemEnqueued.Subscribe(args =>
                    Log.Information("QueueLimit: {QueueLimit}, QueueCount: {QueueCount}", args.QueueLimit,
                        args.QueueCount));
                events.ItemDequeued.Subscribe(args =>
                    Log.Information("QueueLimit: {QueueLimit}, QueueCount: {QueueCount}", args.QueueLimit,
                        args.QueueCount));
                events.ItemProcessing.Subscribe(args =>
                    Log.Information("ConcurrencyLimit: {ConcurrencyLimit}, ConcurrencyItems: {ConcurrencyCount}",
                        args.ConcurrencyLimit, args.ConcurrencyCount));
                events.ItemProcessed.Subscribe(args =>
                    Log.Information("ConcurrencyLimit: {ConcurrencyLimit}, ConcurrencyItems: {ConcurrencyCount}",
                        args.ConcurrencyLimit, args.ConcurrencyCount));
                events.Rejected.Subscribe(args =>
                    Log.Warning("Item rejected with Priority: {Priority}", args.Priority));
            });
        });
    }
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    var hasRedis = !string.IsNullOrEmpty(redisConnectionString);
    switch (enableRateLimiting)
    {
        case true when hasRedis:
            var redis = ConnectionMultiplexer.Connect(redisConnectionString!);

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    // use loopback as a key if remote IP is null. This will group anyone masking their remote IP
                    // into the same bucket making it _worse_ for them to do this and preventing them from
                    // bypassing the global rate limit
                    var remoteIp = ctx.Connection.RemoteIpAddress ?? IPAddress.Loopback;

                    // Get the user ID from the context
                    var userId = ctx.User.GetIdentity();

                    var limiterOptions = new RedisSlidingWindowRateLimiterOptions
                    {
                        ConnectionMultiplexerFactory = () => redis,
                        Window = TimeSpan.FromSeconds(1),
                        PermitLimit = 20
                    };

                    // If we have a logged-in user then we use their ID as the rate limiter key
                    // otherwise we use their IP
                    return userId is not null
                        ? RedisRateLimitPartition.GetSlidingWindowRateLimiter($"GlobalRateLimiter-{userId}",
                            _ => limiterOptions)
                        : RedisRateLimitPartition.GetSlidingWindowRateLimiter($"GlobalRateLimiter-{remoteIp}",
                            _ => limiterOptions);
                });

                // Some sane default policies for reading/writing endpoints.
                // Feel free to add new policies if you need something more unique.
                // Add other policies
                options.AddPolicy(RateLimiterPolicies.List,
                    new EndpointRateLimiterPolicy(50, 5, TimeSpan.FromSeconds(1), redis));
                options.AddPolicy(RateLimiterPolicies.Read,
                    new EndpointRateLimiterPolicy(20, 2, TimeSpan.FromSeconds(1), redis));
                options.AddPolicy(RateLimiterPolicies.Create,
                    new EndpointRateLimiterPolicy(2, 1, TimeSpan.FromSeconds(3), redis));
                options.AddPolicy(RateLimiterPolicies.Update,
                    new EndpointRateLimiterPolicy(2, 1, TimeSpan.FromSeconds(3), redis));
                options.AddPolicy(RateLimiterPolicies.Patch,
                    new EndpointRateLimiterPolicy(10, 1, TimeSpan.FromSeconds(2), redis));
                options.AddPolicy(RateLimiterPolicies.Delete,
                    new EndpointRateLimiterPolicy(2, 1, TimeSpan.FromSeconds(3), redis));
            });
            break;
            
        case true when !hasRedis:
            // If redis is not available, fall back to in-memory rate limiting
            Log.Warning("Redis not available - Rate limiting falling back to in-memory");
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    // use loopback as a key if remote IP is null. This will group anyone masking their remote IP
                    // into the same bucket making it _worse_ for them to do this and preventing them from
                    // bypassing the global rate limit
                    var remoteIp = ctx.Connection.RemoteIpAddress ?? IPAddress.Loopback;

                    // Get the user ID from the context
                    var userId = ctx.User.GetIdentity();

                    var limiterOptions = new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(1),
                        SegmentsPerWindow = 10,
                        PermitLimit = 20,
                        AutoReplenishment = true,
                        QueueLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    };

                    // If we have a logged-in user then we use their ID as the rate limiter key
                    // otherwise we use their IP
                    return userId is not null
                        ? RateLimitPartition.GetSlidingWindowLimiter($"GlobalRateLimiter-{userId}", _ => limiterOptions)
                        : RateLimitPartition.GetSlidingWindowLimiter($"GlobalRateLimiter-{remoteIp}", _ => limiterOptions);
                });

                // Some sane default policies for reading/writing endpoints.
                // Feel free to add new policies if you need something more unique.
                // Add other policies
                options.AddPolicy(RateLimiterPolicies.List, new EndpointRateLimiterPolicy(50, 5, TimeSpan.FromSeconds(1)));
                options.AddPolicy(RateLimiterPolicies.Read, new EndpointRateLimiterPolicy(20, 2, TimeSpan.FromSeconds(1)));
                options.AddPolicy(RateLimiterPolicies.Create, new EndpointRateLimiterPolicy(2, 1, TimeSpan.FromSeconds(3)));
                options.AddPolicy(RateLimiterPolicies.Update, new EndpointRateLimiterPolicy(2, 1, TimeSpan.FromSeconds(3)));
                options.AddPolicy(RateLimiterPolicies.Patch, new EndpointRateLimiterPolicy(10, 1, TimeSpan.FromSeconds(2)));
                options.AddPolicy(RateLimiterPolicies.Delete, new EndpointRateLimiterPolicy(2, 1, TimeSpan.FromSeconds(3)));
            });
            break;
        
        default:
            Log.Warning("Rate limiting disabled");
            break;
    }

    #endregion

    #region OpenAPI

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = false;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        })
        .AddMvc();
    builder.Services.AddSwaggerGen();
    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    #endregion

    #region CORS

    builder.Services.AddCors(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.AddDefaultPolicy(cfg =>
            {
                cfg.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                    .WithMethods("DELETE", "GET", "POST", "PUT", "PATCH", "OPTIONS")
                    .AllowAnyHeader();
            });
        }
        else
        {
            var domains = builder.Configuration.GetSection("CorsDomains").Get<string[]>() ?? [];
            options.AddDefaultPolicy(cfg =>
            {
                cfg.WithOrigins(domains)
                    .WithMethods("DELETE", "GET", "POST", "PUT", "PATCH", "OPTIONS")
                    .AllowAnyHeader();
            });
        }
    });

    #endregion
    
    builder.Services.AddProblemDetails();

    GridifyGlobalConfiguration.IgnoreNotMappedFields = true;
    
    var app = builder.Build();
    await app.AwaitDatabaseReadiness(true, app.Lifetime.ApplicationStopping);
    
    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
        app.UseDeveloperExceptionPage();

    // Load shedding first since if we're under load we don't want to evaluate the rate limiting
    if (enableLoadShedding) app.UseLoadShedding();
    if (enableRateLimiting) app.UseRateLimiter();

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors();
    app.UseSwagger();
    if (app.Environment.IsDevelopment())
        app.UseSwaggerUI(options =>
        {
            options.OAuthClientId(app.Configuration.GetValue<string>("Authentication:SwaggerClientId"));
            options.OAuthUsePkce();
            
            options.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
            {
                {"audience", app.Configuration.GetValue<string>("Authentication:Audience")!}
            });
            options.OAuth2RedirectUrl($"{app.Configuration.GetValue<string>("ApiBaseUrl")}/swagger/oauth2-redirect.html");
        });

    var versionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    var latestVersion = versionProvider.ApiVersionDescriptions.Max(v => v.ApiVersion.MajorVersion);
    foreach (var apiVersionDescription in versionProvider.ApiVersionDescriptions)
    {
        app.UseReDoc(options =>
        {
            options.DocumentTitle = $"TemplateProject API {apiVersionDescription.GroupName}";
            options.ConfigObject.PathInMiddlePanel = true;
            options.ConfigObject.RequiredPropsFirst = true;
            options.ConfigObject.NativeScrollbars = true;
            options.SpecUrl = $"/swagger/v{apiVersionDescription.ApiVersion.MajorVersion}/swagger.json";
            options.RoutePrefix = $"v{apiVersionDescription.ApiVersion.MajorVersion}/docs";
        });
    }

    var options = new RewriteOptions()
        .AddRedirect("^docs$", $"v{latestVersion}/docs")
        .AddRedirect("^docs/$", $"v{latestVersion}/docs")
        .AddRedirect("^docs/(.*)", $"v{latestVersion}/docs");
    app.UseRewriter(options);

    app.UseAuthorization();
    app.UseApiOutputCache();
    
    app.MapControllers();
    app.MapDefaultEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}