using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Required]
    [MaxLength(20)]
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Campaign, Partner, Other

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [MaxLength(20)]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent

    [Range(0, 100000000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedValue { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? Owner { get; set; }

    public string? Notes { get; set; }

    // Conversion tracking
    public int? ConvertedCustomerId { get; set; }
    [ForeignKey("ConvertedCustomerId")]
    public virtual Customer? ConvertedCustomer { get; set; }

    public int? ConvertedOpportunityId { get; set; }
    [ForeignKey("ConvertedOpportunityId")]
    public virtual Opportunity? ConvertedOpportunity { get; set; }

    // Navigation
    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
