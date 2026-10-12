using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class ActivityListViewModel
{
    public List<Activity> Activities { get; set; } = new();
    public string? TypeFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? AssignedFilter { get; set; }
    public SelectList? OwnersList { get; set; }
}

public class ActivityFormViewModel
{
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity Type is required.")]
    [Display(Name = "Activity Type")]
    public string ActivityType { get; set; } = "Call";

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Date & Time is required.")]
    [Display(Name = "Date & Time")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "Completed";

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? OwnersList { get; set; }
}
