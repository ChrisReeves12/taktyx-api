using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Service;

public class MailGunService : IMailService
{
    private readonly string _apiKey;
    private readonly string _mailDomain;
    private readonly HttpClient _httpClient;

    public MailGunService(IConfiguration configuration, HttpClient httpClient)
    {
        _apiKey = configuration.GetValue<string>("MailGunAPIKey") ?? string.Empty;
        _mailDomain = configuration.GetValue<string>("MailGunDomain") ?? string.Empty;
        _httpClient = httpClient;
    }

    public async Task SendMailAsync(string[] recipients, string subject, string message, string? from = null)
    {
        if (recipients == null || recipients.Length == 0)
        {
            throw new ArgumentException("At least one recipient is required", nameof(recipients));
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Mailgun API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_mailDomain))
        {
            throw new InvalidOperationException("Mailgun domain is not configured.");
        }

        var requestUrl = $"https://api.mailgun.net/v3/{_mailDomain}/messages";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        var basicAuthToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{_apiKey}"));
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuthToken);

        using var formData = new MultipartFormDataContent();

        var fromAddress = string.IsNullOrWhiteSpace(from) ? $"support@{_mailDomain}" : from;
        formData.Add(new StringContent(fromAddress), "from");

        foreach (var recipient in recipients)
        {
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                formData.Add(new StringContent(recipient), "to");
            }
        }

        formData.Add(new StringContent(subject ?? string.Empty), "subject");
        formData.Add(new StringContent(message ?? string.Empty), "html");

        requestMessage.Content = formData;

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Mailgun request failed: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
        }
    }
}