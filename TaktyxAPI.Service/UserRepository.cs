using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TaktyxAPI.Data.Data;
using TaktyxAPI.Data.Entities;
using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Service
{
    public class UserRepository : IUserRepository
    {
        private readonly TaktyxDbContext _dbContext;

        public UserRepository(TaktyxDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _dbContext.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetUsersWithinDistanceAsync(double latitude, double longitude, double radiusInMeters)
        {
            var centerPoint = new Point(longitude, latitude) { SRID = 4326 };

            // Primary query using geography STDistance (optimal with spatial index in production)
            var query = _dbContext.Users
                .Where(u => u.Location != null &&
                           u.Location.Distance(centerPoint) <= radiusInMeters)
                .OrderBy(u => u.Location!.Distance(centerPoint));

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersWithinBoundingBoxAsync(double minLat, double maxLat, double minLng, double maxLng)
        {
            // Create a polygon representing the bounding box
            var ring = new LinearRing(new[]
            {
                new Coordinate(minLng, minLat),
                new Coordinate(maxLng, minLat),
                new Coordinate(maxLng, maxLat),
                new Coordinate(minLng, maxLat),
                new Coordinate(minLng, minLat) // Close the ring
            });

            var boundingBox = new Polygon(ring) { SRID = 4326 };

            // Query using geography STIntersects (optimal with spatial index in production)
            var query = _dbContext.Users
                .Where(u => u.Location != null &&
                           u.Location.Intersects(boundingBox));

            return await query.ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            // Auto-create geography point from lat/lng if provided
            if (user.Latitude.HasValue && user.Longitude.HasValue && user.Location == null)
            {
                user.Location = new Point(user.Longitude.Value, user.Latitude.Value) { SRID = 4326 };
            }

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            // Update geography point if lat/lng changed
            if (user.Latitude.HasValue && user.Longitude.HasValue)
            {
                user.Location = new Point(user.Longitude.Value, user.Latitude.Value) { SRID = 4326 };
            }

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _dbContext.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? omitId)
        {
            return omitId is null && await _dbContext.Users.AnyAsync(u => u.Email.Equals(email.ToLower())) || omitId is not null
                && await _dbContext.Users.AnyAsync(u => u.Email.Equals(email.ToLower()) && u.Id != omitId);
        }
    }
}
