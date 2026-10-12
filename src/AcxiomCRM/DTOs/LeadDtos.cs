using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs;

public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public int? ConvertedOpportunityId { get; set; }
}

public class CreateLeadDto
{
    [Required]
    [StringLength(150)]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string Source { get; set; } = "Website";
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";

    [Range(0, 100000000)]
    public decimal ExpectedValue { get; set; }

    public string? AssignedTo { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLeadDto : CreateLeadDto
{
}

public class ConvertLeadDto
{
    public bool CreateOpportunity { get; set; } = true;
    public string? OpportunityName { get; set; }
    public decimal OpportunityAmount { get; set; } = 1000;
    public string OpportunityStage { get; set; } = "Qualification";
    public int Probability { get; set; } = 20;
    public DateTime? ExpectedCloseDate { get; set; }
}
