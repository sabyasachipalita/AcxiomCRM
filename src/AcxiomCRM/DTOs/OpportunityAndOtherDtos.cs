using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs;

public class OpportunityDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedAmount => Math.Round(Amount * Probability / 100m, 2);
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateOpportunityDto
{
    [Required]
    [StringLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }

    [Required]
    [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public string Stage { get; set; } = "Qualification";

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 20;

    [Required]
    public DateTime ExpectedCloseDate { get; set; }

    public string Status { get; set; } = "Open";
    public string? AssignedTo { get; set; }
    public string? Notes { get; set; }
}

public class UpdateOpportunityDto : CreateOpportunityDto
{
}

public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public DateTime FollowUpDate { get; set; }
    public string FollowUpType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
}

public class CreateFollowUpDto
{
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    [Required]
    public string FollowUpType { get; set; } = "Call";

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public string Status { get; set; } = "Planned";
    public string? AssignedTo { get; set; }
}

public class LoginRequestDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
