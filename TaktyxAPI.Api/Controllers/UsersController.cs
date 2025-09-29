using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaktyxAPI.Api.Extensions;
using TaktyxAPI.Data.Data;
using TaktyxAPI.Data.Entities;
using TaktyxAPI.DTO;
using TaktyxAPI.Service.Interfaces;

namespace TaktyxAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly TaktyxDbContext _dbContext;
        private readonly IPasswordService _passwordService;
        private readonly IUserRepository _userRepository;

        public UsersController(TaktyxDbContext dbContext, IPasswordService passwordService,
            IUserRepository userRepository)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
            _userRepository = userRepository;
        }

        [HttpGet("exists")]
        public async Task<ActionResult> UserEmailExists([FromQuery] string email, [FromQuery] int? omitId)
        {
            return (await _userRepository.ExistsByEmailAsync(email, omitId)) ? UnprocessableEntity() : Ok();
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

            var user = await _userRepository.CreateAsync(new User
            {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Email = normalizedEmail,
                Password = _passwordService.HashPassword(createUserDto.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

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
            var user = await _userRepository.GetByIdAsync(id);
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

            var user = await _userRepository.GetByIdAsync(userId.Value);
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

        [HttpPut("{userId:int}/skills")]
        [Authorize]
        public async Task<ActionResult<UserSkillResponseDto>> AssignSkillToUser(int userId, AssignUserSkillRequestDto request)
        {
            try
            {
                // Get current user ID from token
                var currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");

                // Check if user is trying to assign skill to themselves or if they're an admin
                if (currentUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid("You can only assign skills to yourself.");
                }

                // Verify the user exists
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }

                // Verify the skill exists and load its fields
                var skill = await _dbContext.Skills
                    .Include(s => s.SkillFields)
                    .ThenInclude(sf => sf.SkillFieldChoices)
                    .FirstOrDefaultAsync(s => s.Id == request.SkillId);

                if (skill == null)
                {
                    return NotFound($"Skill with ID {request.SkillId} not found.");
                }

                // Check if user already has this skill assigned
                var existingUserSkill = await _dbContext.UserSkills
                    .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == request.SkillId);

                if (existingUserSkill != null)
                {
                    return Conflict($"User already has the skill '{skill.Name}' assigned.");
                }

                // Validate all provided field values
                var fieldValueErrors = new List<string>();
                var providedFieldIds = request.FieldValues.Select(fv => fv.SkillFieldId).ToList();

                foreach (var skillField in skill.SkillFields)
                {
                    var providedValue = request.FieldValues.FirstOrDefault(fv => fv.SkillFieldId == skillField.Id);

                    // Check required fields
                    if (skillField.Required && providedValue == null)
                    {
                        fieldValueErrors.Add($"Required field '{skillField.Name}' is missing.");
                        continue;
                    }

                    if (providedValue != null)
                    {
                        // Validate field type matches provided value
                        var validationError = ValidateFieldValue(skillField, providedValue);
                        if (!string.IsNullOrEmpty(validationError))
                        {
                            fieldValueErrors.Add(validationError);
                        }
                    }
                }

                // Check for field values that don't belong to this skill
                var invalidFieldIds = providedFieldIds.Except(skill.SkillFields.Select(sf => sf.Id)).ToList();
                if (invalidFieldIds.Count > 0)
                {
                    fieldValueErrors.Add($"Field IDs {string.Join(", ", invalidFieldIds)} do not belong to skill '{skill.Name}'.");
                }

                if (fieldValueErrors.Count > 0)
                {
                    return BadRequest($"Validation errors: {string.Join(" ", fieldValueErrors)}");
                }

                // Create UserSkill
                var userSkill = new UserSkill
                {
                    UserId = userId,
                    SkillId = request.SkillId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.UserSkills.Add(userSkill);
                await _dbContext.SaveChangesAsync();

                // Create SkillFieldValues and handle multi-select
                await CreateSkillFieldValues(userSkill, request.FieldValues, skill);

                // Return the created user skill with all data
                var responseDto = await GetUserSkillResponse(userSkill.Id);
                return CreatedAtAction(nameof(GetUserSkill), new { userId = userId, userSkillId = userSkill.Id }, responseDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while assigning the skill: {ex.Message}");
            }
        }

        [HttpGet("{userId:int}/skills")]
        [Authorize]
        public async Task<ActionResult<List<UserSkillResponseDto>>> GetUserSkills(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");

            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid("You can only view your own skills unless you are an admin.");
            }

            var userSkills = await _dbContext.UserSkills
                .Include(us => us.Skill)
                .Include(us => us.SkillFieldValues)
                .ThenInclude(sfv => sfv.SkillField)
                .Include(us => us.SkillFieldValues)
                .ThenInclude(sfv => sfv.SkillFieldValueChoices)
                .ThenInclude(sfvc => sfvc.SkillFieldChoice)
                .Where(us => us.UserId == userId)
                .ToListAsync();

            var responseDtos = userSkills.Select(us => new UserSkillResponseDto
            {
                Id = us.Id,
                UserId = us.UserId,
                SkillId = us.SkillId,
                SkillName = us.Skill.Name,
                SkillMachineName = us.Skill.MachineName,
                CreatedAt = us.CreatedAt,
                UpdatedAt = us.UpdatedAt,
                FieldValues = us.SkillFieldValues.Select(sfv => new UserSkillFieldValueResponseDto
                {
                    Id = sfv.Id,
                    SkillFieldId = sfv.SkillFieldId,
                    FieldName = sfv.SkillField.Name,
                    FieldMachineName = sfv.SkillField.MachineName,
                    FieldType = sfv.SkillField.FieldType.ToString(),
                    TextInputValue = sfv.TextInputValue,
                    TextAreaValue = sfv.TextAreaValue,
                    IntegerValue = sfv.IntegerValue,
                    DecimalValue = sfv.DecimalValue,
                    DateTimeValue = sfv.DateTimeValue,
                    BooleanValue = sfv.BooleanValue,
                    SelectedValues = GetSelectedValues(sfv)
                }).ToList()
            }).ToList();

            return Ok(responseDtos);
        }

        private async Task<ActionResult<UserSkillResponseDto>> GetUserSkill(int userId, int userSkillId)
        {
            var currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");

            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid("You can only view your own skills unless you are an admin.");
            }

            var responseDto = await GetUserSkillResponse(userSkillId);
            if (responseDto == null)
            {
                return NotFound();
            }

            return Ok(responseDto);
        }

        private async Task CreateSkillFieldValues(UserSkill userSkill, List<AssignSkillFieldValueDto> fieldValues, Skill skill)
        {
            var skillFieldValues = new List<SkillFieldValue>();

            foreach (var fieldValueDto in fieldValues)
            {
                var skillField = skill.SkillFields.First(sf => sf.Id == fieldValueDto.SkillFieldId);

                var skillFieldValue = new SkillFieldValue
                {
                    SkillFieldId = fieldValueDto.SkillFieldId,
                    UserSkillId = userSkill.Id,
                    TextInputValue = fieldValueDto.TextInputValue,
                    TextAreaValue = fieldValueDto.TextAreaValue,
                    IntegerValue = fieldValueDto.IntegerValue,
                    DecimalValue = fieldValueDto.DecimalValue,
                    DateTimeValue = fieldValueDto.DateTimeValue,
                    BooleanValue = fieldValueDto.BooleanValue,
                    DropdownValue = skillField.FieldType == FieldType.Dropdown ? fieldValueDto.SelectedValues?.FirstOrDefault() : null,
                    RadioSelectionValue = skillField.FieldType == FieldType.RadioSelection ? fieldValueDto.SelectedValues?.FirstOrDefault() : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                skillFieldValues.Add(skillFieldValue);
            }

            if (skillFieldValues.Count > 0)
            {
                _dbContext.SkillFieldValues.AddRange(skillFieldValues);
                await _dbContext.SaveChangesAsync();

                // Handle multi-select values
                var skillFieldValueChoices = new List<SkillFieldValueChoice>();

                for (var i = 0; i < fieldValues.Count; i++)
                {
                    var fieldValueDto = fieldValues[i];
                    var skillField = skill.SkillFields.First(sf => sf.Id == fieldValueDto.SkillFieldId);
                    var skillFieldValue = skillFieldValues[i];

                    if (skillField.FieldType == FieldType.MultiSelect && fieldValueDto.SelectedValues?.Any() == true)
                    {
                        foreach (var choiceMachineName in fieldValueDto.SelectedValues)
                        {
                            var choice = skillField.SkillFieldChoices
                                .FirstOrDefault(c => c.MachineName == choiceMachineName);

                            if (choice != null)
                            {
                                skillFieldValueChoices.Add(new SkillFieldValueChoice
                                {
                                    SkillFieldValueId = skillFieldValue.Id,
                                    SkillFieldChoiceId = choice.Id,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                }

                if (skillFieldValueChoices.Count > 0)
                {
                    _dbContext.SkillFieldValueChoices.AddRange(skillFieldValueChoices);
                    await _dbContext.SaveChangesAsync();
                }
            }
        }

        private string? ValidateFieldValue(SkillField skillField, AssignSkillFieldValueDto fieldValue)
        {
            switch (skillField.FieldType)
            {
                case FieldType.TextInput:
                    if (string.IsNullOrEmpty(fieldValue.TextInputValue))
                        return $"Text input value required for field '{skillField.Name}'.";
                    break;
                case FieldType.TextArea:
                    if (string.IsNullOrEmpty(fieldValue.TextAreaValue))
                        return $"Text area value required for field '{skillField.Name}'.";
                    break;
                case FieldType.Integer:
                    if (!fieldValue.IntegerValue.HasValue)
                        return $"Integer value required for field '{skillField.Name}'.";
                    break;
                case FieldType.Decimal:
                    if (!fieldValue.DecimalValue.HasValue)
                        return $"Decimal value required for field '{skillField.Name}'.";
                    break;
                case FieldType.DateTime:
                    if (!fieldValue.DateTimeValue.HasValue)
                        return $"DateTime value required for field '{skillField.Name}'.";
                    break;
                case FieldType.Boolean:
                    if (!fieldValue.BooleanValue.HasValue)
                        return $"Boolean value required for field '{skillField.Name}'.";
                    break;
                case FieldType.Dropdown:
                    if (fieldValue.SelectedValues?.Count != 1)
                        return $"Dropdown field '{skillField.Name}' requires exactly one selection.";

                    if (skillField.SkillFieldChoices.All(c => c.MachineName != fieldValue.SelectedValues[0]))
                        return $"Invalid dropdown value '{fieldValue.SelectedValues[0]}' for field '{skillField.Name}'.";
                    break;
                case FieldType.RadioSelection:
                    if (fieldValue.SelectedValues?.Count != 1)
                        return $"Radio selection field '{skillField.Name}' requires exactly one selection.";

                    if (skillField.SkillFieldChoices.All(c => c.MachineName != fieldValue.SelectedValues[0]))
                        return $"Invalid radio selection value '{fieldValue.SelectedValues[0]}' for field '{skillField.Name}'.";
                    break;
                case FieldType.MultiSelect:
                    if (fieldValue.SelectedValues?.Count < 1)
                        return $"Multi-select field '{skillField.Name}' requires at least one selection.";

                    if (fieldValue.SelectedValues != null)
                    {
                        var invalidChoices = fieldValue.SelectedValues.Where(sv => skillField.SkillFieldChoices.All(c => c.MachineName != sv)).ToList();
                        if (invalidChoices.Count != 0)
                            return $"Invalid multi-select values {string.Join(", ", invalidChoices)} for field '{skillField.Name}'.";
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }

        private async Task<UserSkillResponseDto?> GetUserSkillResponse(int userSkillId)
        {
            var userSkill = await _dbContext.UserSkills
                .Include(us => us.Skill)
                .Include(us => us.SkillFieldValues)
                .ThenInclude(sfv => sfv.SkillField)
                .Include(us => us.SkillFieldValues)
                .ThenInclude(sfv => sfv.SkillFieldValueChoices)
                .ThenInclude(sfvc => sfvc.SkillFieldChoice)
                .FirstOrDefaultAsync(us => us.Id == userSkillId);

            if (userSkill == null) return null;

            return new UserSkillResponseDto
            {
                Id = userSkill.Id,
                UserId = userSkill.UserId,
                SkillId = userSkill.SkillId,
                SkillName = userSkill.Skill.Name,
                SkillMachineName = userSkill.Skill.MachineName,
                CreatedAt = userSkill.CreatedAt,
                UpdatedAt = userSkill.UpdatedAt,
                FieldValues = userSkill.SkillFieldValues.Select(sfv => new UserSkillFieldValueResponseDto
                {
                    Id = sfv.Id,
                    SkillFieldId = sfv.SkillFieldId,
                    FieldName = sfv.SkillField.Name,
                    FieldMachineName = sfv.SkillField.MachineName,
                    FieldType = sfv.SkillField.FieldType.ToString(),
                    TextInputValue = sfv.TextInputValue,
                    TextAreaValue = sfv.TextAreaValue,
                    IntegerValue = sfv.IntegerValue,
                    DecimalValue = sfv.DecimalValue,
                    DateTimeValue = sfv.DateTimeValue,
                    BooleanValue = sfv.BooleanValue,
                    SelectedValues = GetSelectedValues(sfv)
                }).ToList()
            };
        }

        private List<string>? GetSelectedValues(SkillFieldValue sfv)
        {
            switch (sfv.SkillField.FieldType)
            {
                case FieldType.Dropdown:
                    return !string.IsNullOrEmpty(sfv.DropdownValue) ? [sfv.DropdownValue] : null;
                case FieldType.RadioSelection:
                    return !string.IsNullOrEmpty(sfv.RadioSelectionValue) ? [sfv.RadioSelectionValue] : null;
                case FieldType.MultiSelect:
                    return sfv.SkillFieldValueChoices?.Select(sfvc => sfvc.SkillFieldChoice.MachineName).ToList();
                default:
                    return null;
            }
        }
    }
}