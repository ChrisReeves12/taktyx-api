using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaktyxAPI.Data.Entities
{
    public enum FieldType
    {
        Dropdown = 0,
        TextInput = 1,
        TextArea = 2,
        Integer = 3,
        Decimal = 4,
        RadioSelection = 5,
        DateTime = 6,
        Boolean = 7,
        MultiSelect = 8
    }

    public class SkillField
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        public int SkillId { get; set; }

        [Required]
        public FieldType FieldType { get; set; }

        [Required]
        public bool Required { get; set; }

        [MaxLength(100)]
        public string? DefaultValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SkillId")]
        public virtual Skill Skill { get; set; } = null!;

        public virtual ICollection<SkillFieldValue> SkillFieldValues { get; set; } = new List<SkillFieldValue>();
    }
}
