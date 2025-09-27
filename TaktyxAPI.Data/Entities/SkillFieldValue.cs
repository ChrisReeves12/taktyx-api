using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaktyxAPI.Data.Entities
{
    public class SkillFieldValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SkillFieldId { get; set; }

        [Required]
        public int UserSkillId { get; set; }

        // Value fields for different types
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DecimalValue { get; set; }

        [MaxLength(300)]
        public string? TextInputValue { get; set; }

        [Column(TypeName = "text")]
        public string? TextAreaValue { get; set; }

        public int? IntegerValue { get; set; }

        public DateTime? DateTimeValue { get; set; }

        public bool? BooleanValue { get; set; }

        // For dropdown and radio selection values
        [MaxLength(100)]
        public string? DropdownValue { get; set; }

        [MaxLength(100)]
        public string? RadioSelectionValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SkillFieldId")]
        public virtual SkillField SkillField { get; set; } = null!;

        [ForeignKey("UserSkillId")]
        public virtual UserSkill UserSkill { get; set; } = null!;

        public virtual ICollection<SkillFieldValueChoice> SkillFieldValueChoices { get; set; } = 
            new List<SkillFieldValueChoice>();
    }
}
