using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class FollowUpListViewModel
{
    public List<FollowUp> FollowUps { get; set; } = new();
    public string ActiveTab { get; set; } = "all"; // all, upcoming, overdue, completed
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
    public string? AssignedFilter { get; set; }
    public SelectList? OwnersList { get; set; }
}

public class FollowUpFormViewModel
{
    public int FollowUpId { get; set; }

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Related Opportunity")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required(ErrorMessage = "Follow-up Type is required.")]
    [Display(Name = "Type")]
    public string FollowUpType { get; set; } = "Call";

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    public string? Remarks { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "Planned";

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? OpportunitiesList { get; set; }
    public SelectList? OwnersList { get; set; }
}
