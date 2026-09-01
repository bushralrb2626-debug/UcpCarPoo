using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UcpCarPool.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body)
        {
            var tenantId =
                _configuration["MicrosoftGraph:TenantId"];

            var clientId =
                _configuration["MicrosoftGraph:ClientId"];

            var clientSecret =
                _configuration["MicrosoftGraph:ClientSecret"];

            var senderEmail =
                _configuration["MicrosoftGraph:SenderEmail"];

            if (string.IsNullOrWhiteSpace(tenantId) ||
                string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Microsoft Graph email settings are not configured.");
            }

            // Get access token
            var tokenClient =
                _httpClientFactory.CreateClient();

            var tokenRequest =
                new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["scope"] = "https://graph.microsoft.com/.default",
                    ["grant_type"] = "client_credentials"
                };

            using var tokenContent =
                new FormUrlEncodedContent(tokenRequest);

            var tokenResponse =
                await tokenClient.PostAsync(
                    $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                    tokenContent);

            var tokenJson =
                await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Microsoft identity token request failed: {tokenJson}");
            }

            using var tokenDocument =
                JsonDocument.Parse(tokenJson);

            var accessToken =
                tokenDocument
                    .RootElement
                    .GetProperty("access_token")
                    .GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException(
                    "Microsoft Graph access token was not returned.");
            }

            // Email body
            var message = new
            {
                message = new
                {
                    subject = subject,

                    body = new
                    {
                        contentType = "HTML",
                        content = body
                    },

                    toRecipients = new[]
                    {
                        new
                        {
                            emailAddress = new
                            {
                                address = toEmail
                            }
                        }
                    }
                },

                saveToSentItems = true
            };

            var json =
                JsonSerializer.Serialize(message);

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var graphClient =
                _httpClientFactory.CreateClient();

            graphClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            var response =
                await graphClient.PostAsync(
                    $"https://graph.microsoft.com/v1.0/users/{senderEmail}/sendMail",
                    content);

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                throw new InvalidOperationException(
                    $"Microsoft Graph email sending failed: {error}");
            }
        }
    }
}