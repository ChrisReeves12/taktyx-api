using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using TaktyxAPI.Service.Interfaces;
using System.Security.Cryptography;

namespace TaktyxAPI.Service;

public class MailGunService : IMailService
{
    private readonly string _apiKey;
    private readonly string _mailDomain;
    private readonly HttpClient _httpClient;
    private readonly string _appName;

    public MailGunService(IConfiguration configuration, HttpClient httpClient)
    {
        _apiKey = configuration.GetValue<string>("MailGunAPIKey") ?? string.Empty;
        _mailDomain = configuration.GetValue<string>("MailGunDomain") ?? string.Empty;
        _httpClient = httpClient;
        _appName = configuration.GetValue<string>("AppName") ?? string.Empty;
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

    public string GenerateOtp(int numOfDigits)
    {
        if (numOfDigits <= 0 || numOfDigits > 18)
        {
            throw new ArgumentOutOfRangeException(nameof(numOfDigits), "numOfDigits must be between 1 and 18");
        }

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt64(bytes, 0);

        var modulus = (ulong)Math.Pow(10, numOfDigits);
        var number = value % modulus;
        return number.ToString($"D{numOfDigits}");
    }

    public async Task SendPasswordResetOtpAsync(string email, string otp, int expiresInMinutes)
    {
        var subject = $"Your {_appName} password reset code";
        var html = $"<p>Your password reset code is: <strong>{otp}</strong></p><p>This code expires in {expiresInMinutes} minutes.</p>";
        await SendMailAsync([email], subject, html);
    }
}