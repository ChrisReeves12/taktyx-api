using TaktyxAPI.Data.Entities;
using TaktyxAPI.DTO;

namespace TaktyxAPI.Service.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetUsersWithinDistanceAsync(double latitude, double longitude, double radiusInMeters);
        Task<IEnumerable<User>> GetUsersWithinBoundingBoxAsync(double minLat, double maxLat, double minLng, double maxLng);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByEmailAsync(string email, int? omitId = null);
    }
}
