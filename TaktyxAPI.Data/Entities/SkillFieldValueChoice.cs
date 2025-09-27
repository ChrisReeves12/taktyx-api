using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaktyxAPI.Data.Entities;

[Table("SkillFieldValueChoices")]
public class SkillFieldValueChoice
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int SkillFieldValueId { get; set; }
    
    [Required]
    public int SkillFieldChoiceId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("SkillFieldValueId")]
    public virtual SkillFieldValue SkillFieldValue { get; set; }
    
    [ForeignKey("SkillFieldChoiceId")]
    public virtual SkillFieldChoice SkillFieldChoice { get; set; }
}