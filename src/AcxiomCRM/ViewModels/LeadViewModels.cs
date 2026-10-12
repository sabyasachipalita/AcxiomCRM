using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class LeadListViewModel
{
    public List<Lead> Leads { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? PriorityFilter { get; set; }
    public string? AssignedFilter { get; set; }
    public SelectList? OwnersList { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class LeadFormViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "Phone number must be between 7 and 20 characters.")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Source is required.")]
    public string Source { get; set; } = "Website";

    [Required(ErrorMessage = "Lead Status is required.")]
    public string Status { get; set; } = "New";

    [Required(ErrorMessage = "Priority is required.")]
    public string Priority { get; set; } = "Medium";

    [Range(0, 100000000, ErrorMessage = "Expected Value must be a positive number.")]
    [Display(Name = "Expected Value ($)")]
    public decimal ExpectedValue { get; set; } = 0;

    [Display(Name = "Assign To")]
    public string? AssignedTo { get; set; }

    public string? Notes { get; set; }

    public SelectList? OwnersList { get; set; }
}

public class LeadDetailsViewModel
{
    public Lead Lead { get; set; } = null!;
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}

public class LeadConvertViewModel
{
    public int LeadId { get; set; }

    public string LeadName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Create Sales Opportunity?")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal OpportunityAmount { get; set; } = 1000;

    public string OpportunityStage { get; set; } = "Qualification";

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 25;

    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddMonths(1);

    public string? AssignedTo { get; set; }
    public SelectList? OwnersList { get; set; }
}
