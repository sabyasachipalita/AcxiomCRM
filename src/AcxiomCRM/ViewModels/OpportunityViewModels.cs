using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class OpportunityListViewModel
{
    public List<Opportunity> Opportunities { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StageFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? AssignedFilter { get; set; }
    public SelectList? OwnersList { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
}

public class OpportunityFormViewModel
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Amount ($)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    public string Stage { get; set; } = "Qualification";

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 25;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "Open";

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public string? Notes { get; set; }

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? OwnersList { get; set; }
}

public class OpportunityDetailsViewModel
{
    public Opportunity Opportunity { get; set; } = null!;
    public List<FollowUp> FollowUps { get; set; } = new();
}
