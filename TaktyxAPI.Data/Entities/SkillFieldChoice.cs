using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaktyxAPI.Data.Entities;

[Table("SkillFieldChoices")]
public class SkillFieldChoice
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int SkillFieldId { get; set; }
    
    [MaxLength(300)]
    [Required]
    public string Name { get; set; }
    
    [MaxLength(300)]
    [Required]
    public string MachineName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("SkillFieldId")]
    public virtual SkillField SkillField { get; set; }
}