using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaktyxAPI.Data.Data;
using TaktyxAPI.Data.Entities;
using TaktyxAPI.DTO;
using TaktyxAPI.DTO.Constants;

namespace TaktyxAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly TaktyxDbContext _dbContext;
        private readonly ILogger<SkillsController> _logger;

        public SkillsController(TaktyxDbContext dbContext, ILogger<SkillsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<SkillResponseDto>> CreateSkill(CreateSkillRequestDto request)
        {
            try
            {
                // Check if skill with this machine name already exists
                var existingSkill = await _dbContext.Skills
                    .FirstOrDefaultAsync(s => s.MachineName.Equals(request.MachineName));

                if (existingSkill != null)
                {
                    return Conflict($"A skill with machine name '{request.MachineName}' already exists.");
                }

                // Validate field types
                var validFieldTypes = Enum.GetNames(typeof(FieldType));
                foreach (var field in request.Fields)
                {
                    if (!validFieldTypes.Contains(field.Type, StringComparer.OrdinalIgnoreCase))
                    {
                        return BadRequest($"Invalid field type '{field.Type}'. Valid types are: {string.Join(", ", validFieldTypes)}");
                    }

                    // Check for duplicate machine names within the skill
                    var duplicateField = request.Fields
                        .Where(f => f.MachineName.Equals(field.MachineName))
                        .Skip(1)
                        .Any();

                    if (duplicateField)
                    {
                        return BadRequest($"Duplicate field machine name '{field.MachineName}' within the skill.");
                    }
                }

                var skill = new Skill
                {
                    Name = request.Name,
                    MachineName = request.MachineName,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.Skills.Add(skill);
                await _dbContext.SaveChangesAsync();

                // Create the skill fields
                var skillFields = new List<SkillField>();
                foreach (var fieldDto in request.Fields)
                {
                    if (!Enum.TryParse<FieldType>(fieldDto.Type, true, out var fieldType))
                    {
                        return BadRequest($"Invalid field type '{fieldDto.Type}'");
                    }

                    var skillField = new SkillField
                    {
                        Name = fieldDto.Name,
                        MachineName = fieldDto.MachineName,
                        SkillId = skill.Id,
                        FieldType = fieldType,
                        Required = fieldDto.Required,
                        DefaultValue = fieldDto.DefaultValue,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    skillFields.Add(skillField);

                    // Add dropdown and multi-select choices
                    if (fieldType is FieldType.MultiSelect or FieldType.RadioSelection or FieldType.Dropdown && fieldDto.SkillFieldChoices.Count > 0)
                    {
                        foreach (var fieldChoice in fieldDto.SkillFieldChoices)
                        {
                            if (await _dbContext.SkillFieldChoices.AnyAsync(sfc =>
                                    sfc.MachineName.Equals(fieldChoice.MachineName)))
                            {
                                continue;
                            }

                            _dbContext.SkillFieldChoices.Add(new SkillFieldChoice
                            {
                                Name = fieldChoice.Name,
                                MachineName = fieldChoice.MachineName,
                                SkillField = skillField
                            });
                        }
                    }
                }

                if (skillFields.Count != 0)
                {
                    _dbContext.SkillFields.AddRange(skillFields);
                    await _dbContext.SaveChangesAsync();
                }

                // Return the created skill with fields
                var responseDto = MapToResponseDto(skill, skillFields);
                return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating skill");
                return StatusCode(500, "An error occurred while creating the skill.");
            }
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<SkillResponseDto>> GetSkill(int id)
        {
            var skill = await _dbContext.Skills
                .Include(s => s.SkillFields)
                .ThenInclude(sf => sf.SkillFieldChoices)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (skill == null)
            {
                return NotFound();
            }

            var responseDto = MapToResponseDto(skill, skill.SkillFields.ToList());
            return Ok(responseDto);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<SkillResponseDto>>> GetSkills()
        {
            var skills = await _dbContext.Skills
                .Include(s => s.SkillFields)
                .ThenInclude(sf => sf.SkillFieldChoices)
                .ToListAsync();

            var responseDtos = skills.Select(s => MapToResponseDto(s, s.SkillFields.ToList())).ToList();
            return Ok(responseDtos);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> DeleteSkill(int id)
        {
            try
            {
                var skill = await _dbContext.Skills
                    .Include(s => s.SkillFields)
                    .Include(s => s.UserSkills)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (skill == null)
                {
                    return NotFound($"Skill with ID {id} not found.");
                }

                // Check if skill is being used by users
                if (skill.UserSkills.Count > 0)
                {
                    return BadRequest($"Cannot delete skill '{skill.Name}' because it is being used by {skill.UserSkills.Count} user(s). Remove user associations first.");
                }

                // Delete associated skill fields first (due to foreign key constraints)
                if (skill.SkillFields.Count > 0)
                {
                    _dbContext.SkillFields.RemoveRange(skill.SkillFields);
                }

                // Delete the skill
                _dbContext.Skills.Remove(skill);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Skill '{SkillName}' (ID: {SkillId}) deleted by user", skill.Name, skill.Id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting skill with ID {SkillId}", id);
                return StatusCode(500, "An error occurred while deleting the skill.");
            }
        }

        private SkillResponseDto MapToResponseDto(Skill skill, List<SkillField> skillFields)
        {
            return new SkillResponseDto
            {
                Id = skill.Id,
                Name = skill.Name,
                MachineName = skill.MachineName,
                Description = skill.Description,
                CreatedAt = skill.CreatedAt,
                UpdatedAt = skill.UpdatedAt,
                Fields = skillFields.Select(sf => new SkillFieldResponseDto
                {
                    Id = sf.Id,
                    Name = sf.Name,
                    MachineName = sf.MachineName,
                    Type = sf.FieldType.ToString(),
                    Required = sf.Required,
                    DefaultValue = sf.DefaultValue,
                    CreatedAt = sf.CreatedAt,
                    UpdatedAt = sf.UpdatedAt,
                    Choices = sf.FieldType is FieldType.Dropdown or FieldType.RadioSelection or FieldType.MultiSelect
                        ? sf.SkillFieldChoices?.Select(sfc => new SkillFieldChoiceResponseDto
                        {
                            Id = sfc.Id,
                            Name = sfc.Name,
                            MachineName = sfc.MachineName
                        }).ToList()
                        : null
                }).ToList()
            };
        }
    }
}
