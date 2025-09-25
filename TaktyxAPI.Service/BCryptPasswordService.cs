using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Service;

public class BCryptPasswordService : IPasswordService
{
    public string HashPassword(string clearTextPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(clearTextPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string clearTextPassword, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(clearTextPassword, passwordHash);
    }
}