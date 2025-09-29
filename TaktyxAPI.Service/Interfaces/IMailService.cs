namespace TaktyxAPI.Service.Interfaces;

public interface IMailService
{
    public Task SendMailAsync(string[] recipients, string subject, string message, string? from = null);
    public string GenerateOtp(int numOfDigits);
    public Task SendPasswordResetOtpAsync(string email, string otp, int expiresInMinutes);
}