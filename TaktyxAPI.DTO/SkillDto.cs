using System.ComponentModel.DataAnnotations;

namespace TaktyxAPI.DTO
{
    public class CreateSkillRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public List<CreateSkillFieldDto> Fields { get; set; } = [];
    }

    public class SkillFieldChoiceDto
    {
        public string Name { get; set; }
        public string MachineName { get; set; }
    }

    public class CreateSkillFieldDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty; // Will map to FieldType enum

        public bool Required { get; set; } = false;

        [MaxLength(100)]
        public string? DefaultValue { get; set; }

        public List<SkillFieldChoiceDto>? SkillFieldChoices { get; set; }
    }

    public class SkillResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<SkillFieldResponseDto> Fields { get; set; } = new List<SkillFieldResponseDto>();
    }

    public class SkillFieldResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string? DefaultValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // For Dropdown, RadioSelection, and MultiSelect field types
        public List<SkillFieldChoiceResponseDto>? Choices { get; set; }
    }

    public class SkillFieldChoiceResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
    }
}
