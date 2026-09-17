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
            var apiKey = _configuration["Brevo:ApiKey"];
            var senderEmail = _configuration["Brevo:SenderEmail"];

            if (string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Brevo email settings are not configured.");
            }

            var message = new
            {
                sender = new
                {
                    email = senderEmail
                },

                to = new[]
                {
                    new
                    {
                        email = toEmail
                    }
                },

                subject = subject,

                htmlContent = body
            };

            var json = JsonSerializer.Serialize(message);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            client.DefaultRequestHeaders.Add(
                "api-key",
                apiKey);

            var response = await client.PostAsync(
                "https://api.brevo.com/v3/smtp/email",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                throw new InvalidOperationException(
                    $"Brevo email sending failed: {error}");
            }
        }
    }
}