using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Opportunity
{
    [Key]
    public int OpportunityId { get; set; }

    [Required]
    [MaxLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Required]
    [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 20;

    [Required]
    public DateTime ExpectedCloseDate { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Open"; // Open, Won, Lost

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? AssignedTo { get; set; }
    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? Owner { get; set; }

    public string? Notes { get; set; }

    // Derived weighted pipeline
    [NotMapped]
    public decimal WeightedAmount => Math.Round(Amount * Probability / 100m, 2);

    // Navigation
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
