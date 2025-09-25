namespace TaktyxAPI.Service.Interfaces;

public interface IPasswordService
{
    public string HashPassword(string clearTextPassword);
    public bool VerifyPassword(string clearTextPassword, string passwordHash);
}