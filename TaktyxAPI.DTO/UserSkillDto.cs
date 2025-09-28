using System.ComponentModel.DataAnnotations;

namespace TaktyxAPI.DTO
{
    public class AssignUserSkillRequestDto
    {
        [Required]
        public int SkillId { get; set; }

        public List<AssignSkillFieldValueDto> FieldValues { get; set; } = new List<AssignSkillFieldValueDto>();
    }

    public class AssignSkillFieldValueDto
    {
        [Required]
        public int SkillFieldId { get; set; }

        // Single value fields
        public string? TextInputValue { get; set; }
        public string? TextAreaValue { get; set; }
        public int? IntegerValue { get; set; }
        public decimal? DecimalValue { get; set; }
        public DateTime? DateTimeValue { get; set; }
        public bool? BooleanValue { get; set; }

        // For choice-based fields (Dropdown, RadioSelection, MultiSelect)
        // Dropdown/RadioSelection: single element array
        // MultiSelect: multiple elements array
        public List<string>? SelectedValues { get; set; }
    }

    public class UserSkillResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string SkillMachineName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<UserSkillFieldValueResponseDto> FieldValues { get; set; } = new List<UserSkillFieldValueResponseDto>();
    }

    public class UserSkillFieldValueResponseDto
    {
        public int Id { get; set; }
        public int SkillFieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldMachineName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;

        // Single value fields
        public string? TextInputValue { get; set; }
        public string? TextAreaValue { get; set; }
        public int? IntegerValue { get; set; }
        public decimal? DecimalValue { get; set; }
        public DateTime? DateTimeValue { get; set; }
        public bool? BooleanValue { get; set; }

        // For choice-based fields (Dropdown, RadioSelection, MultiSelect)
        public List<string>? SelectedValues { get; set; }
    }
}
