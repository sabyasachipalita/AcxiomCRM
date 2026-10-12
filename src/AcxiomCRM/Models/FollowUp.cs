using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }
    [ForeignKey("OpportunityId")]
    public virtual Opportunity? Opportunity { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    public string? AssignedTo { get; set; }
    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? Owner { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
