using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Infrastructure.Authentication.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Authentication;

public class KeycloakUserManager(
    HttpClient client,
    IAuthenticationOptions options,
    IDistributedCache cache
) : IUserManager
{
    private class KeycloakUser
    {
        public string Email { get; set; }
        public bool EmailVerified { get; set; }
        public bool Enabled { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Id { get; set; }
        public Dictionary<string, List<string>>? Attributes { get; set; }
    }

    public static void ConfigureHttpClient(IServiceProvider services, HttpClient client)
    {
        var options = services.GetRequiredService<IAuthenticationOptions>();
        var realm = options.Authority?.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
        var url = new UriBuilder(options.Authority!)
        {
            Path = "",
            Query = "",
            Fragment = ""
        };
        client.BaseAddress = new Uri(url.Uri, "/admin/realms/{realm}/users");

        var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient();
        var tokenEndpoint = $"{url.Uri.ToString().TrimEnd('/')}/realms/master/protocol/openid-connect/token";
        var split = options.ManagementClientSecret!.Split(':');

        var requestBody = new Dictionary<string, string>
        {
            {"client_id", options.ManagementClientId!},
            {"client_secret", options.ManagementClientSecret!},
            {"username", split.First()},
            {"password", split.Last()},
            {"grant_type", "password"}
        };

        var content = new FormUrlEncodedContent(requestBody);
        var response = httpClient.PostAsync(tokenEndpoint, content).GetAwaiter().GetResult();

        if (response.IsSuccessStatusCode)
        {
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var tokenResponse = JsonDocument.Parse(responseContent);
            var accessToken = tokenResponse.RootElement.GetProperty("access_token").GetString();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        else
        {
            var errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new Exception($"Error obtaining access token: {response.StatusCode}, {errorContent}");
        }
    }

    private string BaseUrl
    {
        get
        {
            var realm = options.Authority?.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
            var url = new UriBuilder(options.Authority!);
            url.Path = $"/admin/realms/{realm}/users";
            return url.Uri.ToString();
        }
    }

    public async Task<ExternalUser?> GetUser(string externalId)
    {
        var kcUser = await GetUserInternal(externalId);

        return kcUser is null
            ? null
            : new ExternalUser
            {
                Email = kcUser.Email,
                Id = kcUser.Id,
                EmailVerified = kcUser.EmailVerified,
                Name = $"{kcUser.FirstName} {kcUser.LastName}",
                FamilyName = kcUser.LastName,
                GivenName = kcUser.FirstName,
                PreferredUsername = kcUser.Username,
                Enabled = kcUser.Enabled,
                PhoneNumber = kcUser.Attributes?.ContainsKey("phone_number") ?? false
                    ? kcUser.Attributes["phone_number"][0]
                    : null,
                PhoneNumberVerified = (kcUser.Attributes?.ContainsKey("phone_number") ?? false)
                                      && kcUser.Attributes["phone_number_verified"][0]
                                          .Equals("true", StringComparison.OrdinalIgnoreCase),
                CustomData = kcUser.Attributes ?? []
            };
    }

    private const string CacheKeyPrefix = "ExternalUser_";

    private async Task<KeycloakUser?> GetUserInternal(string externalId)
    {
        var cacheKey = $"{CacheKeyPrefix}{externalId}";
        var cachedValue = await cache.GetStringAsync(cacheKey);

        if (cachedValue is not null)
            return JsonSerializer.Deserialize<KeycloakUser>(cachedValue)!;

        var response = await client.GetAsync($"{BaseUrl}/{externalId}");
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            throw new Exception(
                $"Unable to get user with id {externalId} - {response.StatusCode}:{response.ReasonPhrase}");
        }

        var user = await response.Content.ReadFromJsonAsync<KeycloakUser>();

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user));
        return user;
    }

    public async Task<bool> IsEmailVerified(string externalId)
    {
        var user = await GetUser(externalId);
        return user.EmailVerified;
    }

    public async Task<bool> IsEnabled(string externalId)
    {
        var user = await GetUser(externalId);
        return user.Enabled;
    }

    public async Task UpdatePassword(string externalId, string newPassword)
    {
        var credential = new
        {
            type = "password",
            value = newPassword,
            temporary = false,
        };

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{externalId}/reset-password", credential);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Unable to update password for user with id {externalId}");

        var cacheKey = $"{CacheKeyPrefix}{externalId}";
        await cache.RemoveAsync(cacheKey);
    }

    public async Task UpdateEmail(string externalId, string newEmail, bool requireVerification = false)
    {
        // Keycloak only updates changed fields - don't need the full user object
        var kcUser = new
        {
            email = newEmail,
            emailVerified = !requireVerification
        };

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{externalId}", kcUser);
        if (!response.IsSuccessStatusCode)
            throw new Exception(
                $"Unable to update email for user with id {externalId} - {response.StatusCode}:{response.ReasonPhrase}");

        var cacheKey = $"{CacheKeyPrefix}{externalId}";
        await cache.RemoveAsync(cacheKey);
    }

    /// <summary>
    /// Update phone number
    /// </summary>
    /// <remarks>
    /// Keycloak doesn't natively support phone numbers so we fudge it here by adding a custom attribute.
    /// If you want to do phone number verification and SMS MFA you will need to configure this within
    /// Keycloak directly.
    /// </remarks>
    /// <param name="externalId">The user's Keycloak ID</param>
    /// <param name="newPhone">The phone number in E.164 format. <see href="https://en.wikipedia.org/wiki/E.164" /></param>
    /// <param name="requireVerification">Ignored in this Keycloak implementation</param>
    /// <exception cref="Exception"></exception>
    public async Task UpdatePhone(string externalId, string newPhone, bool requireVerification = false)
    {
        var attributes = new Dictionary<string, List<string>>
        {
            {"phone_number", [newPhone]},
            {"phone_number_verified", [(!requireVerification).ToString()]}
        };
        var kcUser = new
        {
            attributes = attributes
        };

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{externalId}", kcUser);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Unable to update phone number for user with id {externalId}");

        var cacheKey = $"{CacheKeyPrefix}{externalId}";
        await cache.RemoveAsync(cacheKey);
    }

    public async Task UpdateUserName(string externalId, string newUserName)
    {
        if (newUserName.Contains('@'))
            await UpdateEmail(externalId, newUserName);

        throw new ArgumentException("Keycloak uses email address as username.");
    }

    public async Task SendEmailVerify(string externalId)
    {
        var kcUser = await GetUserInternal(externalId);

        if (!kcUser.EmailVerified)
        {
            var response = await client.PutAsync($"{BaseUrl}/{externalId}/send-verify-email", null);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Unable to send verification email for user with id {externalId}");
        }
    }

    public async Task DisableUser(string externalId)
    {
        var kcUser = await GetUserInternal(externalId);
        kcUser.Enabled = false;

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{externalId}", kcUser);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Unable to disable user with id {externalId}");

        var cacheKey = $"{CacheKeyPrefix}{externalId}";
        await cache.RemoveAsync(cacheKey);
    }

    public async Task EnableUser(string externalId)
    {
        var kcUser = await GetUserInternal(externalId);
        kcUser.Enabled = true;

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{externalId}", kcUser);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Unable to enable user with id {externalId}");

        var cacheKey = $"{CacheKeyPrefix}{externalId}";
        await cache.RemoveAsync(cacheKey);
    }

    public async Task DeleteUser(string externalId)
    {
        var response = await client.DeleteAsync($"{BaseUrl}/{externalId}");
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Unable to delete user with id {externalId}");

        var cacheKey = $"{CacheKeyPrefix}{externalId}";
        await cache.RemoveAsync(cacheKey);
    }

    public async Task UpdateCustomData(string externalId, Dictionary<string, List<string>> data)
    {
        var kcUser = await GetUserInternal(externalId);
        kcUser.Attributes = data;

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{externalId}", kcUser);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Unable to update custom data for user with id {externalId}");

        var cacheKey = $"{CacheKeyPrefix}{externalId}";
        await cache.RemoveAsync(cacheKey);
    }
}