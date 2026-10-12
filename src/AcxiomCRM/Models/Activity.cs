using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    public string? AssignedTo { get; set; }
    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? Owner { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Completed"; // Scheduled, Completed, Cancelled

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
