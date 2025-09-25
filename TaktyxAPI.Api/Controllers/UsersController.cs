using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaktyxAPI.Api.Extensions;
using TaktyxAPI.Data.Data;
using TaktyxAPI.Data.Entities;
using TaktyxAPI.DTO;
using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase {
        private readonly TaktyxDbContext _dbContext;
        private readonly IPasswordService _passwordService;

        public UsersController(TaktyxDbContext dbContext, IPasswordService passwordService) {
            _dbContext = dbContext;
            _passwordService = passwordService;
        }

        [HttpGet("exists")]
        public async Task<ActionResult> UserEmailExists([FromQuery] string email, [FromQuery] int? omitId)
        {
            if (omitId is null && await _dbContext.Users.AnyAsync(u => u.Email.Equals(email.ToLower())) || omitId is not null 
                && await _dbContext.Users.AnyAsync(u => u.Email.Equals(email.ToLower()) && u.Id != omitId))
            {
                return UnprocessableEntity();
            }

            return Ok();
        }
        
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        {
            var normalizedEmail = createUserDto.Email.ToLower().Trim();
            
            // Check for duplicate
            if (await _dbContext.Users.AnyAsync(u => u.Email.Equals(normalizedEmail)))
            {
                return BadRequest(new
                {
                    Message = "A user with this email already exists",
                    Field = "Email"
                });
            }
            
            var user = new User {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Email = normalizedEmail,
                Password = _passwordService.HashPassword(createUserDto.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Add(user);
            await _dbContext.SaveChangesAsync();
            
            var userDto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
            
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, userDto);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userId = this.GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Invalid UserId" });
            }

            var user = await _dbContext.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }
    }
}