using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class CustomerListViewModel
{
    public List<Customer> Customers { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public SelectList? OwnersList { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class CustomerFormViewModel
{
    public int CustomerId { get; set; }

    [Display(Name = "Customer Code")]
    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

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

    [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
    public string? State { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "Active";

    [Display(Name = "Assigned Sales Executive")]
    public string? OwnerId { get; set; }

    public string? Notes { get; set; }

    public SelectList? OwnersList { get; set; }
}

public class CustomerDetailsViewModel
{
    public Customer Customer { get; set; } = null!;
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}
