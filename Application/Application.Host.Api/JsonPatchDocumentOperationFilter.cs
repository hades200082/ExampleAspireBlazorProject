using Microsoft.AspNetCore.JsonPatch;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Application.Host.Api;

public class JsonPatchDocumentOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var patchParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(JsonPatchDocument<>));

        foreach (var parameter in patchParameters)
        {
            var schema = new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["op"] = new OpenApiSchema { Type = "string", Enum = new List<IOpenApiAny>
                        {
                            new OpenApiString("add"),
                            new OpenApiString("remove"),
                            new OpenApiString("replace"),
                            new OpenApiString("move"),
                            new OpenApiString("copy"),
                            new OpenApiString("test")
                        }},
                        ["path"] = new OpenApiSchema { Type = "string" },
                        ["from"] = new OpenApiSchema { Type = "string" },
                        ["value"] = new OpenApiSchema { Type = "object" }
                    }
                }
            };

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                },
                Required = true
            };
        }
    }
}