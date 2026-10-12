using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
    public virtual ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
    public virtual ICollection<Opportunity> AssignedOpportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> AssignedFollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> AssignedActivities { get; set; } = new List<Activity>();
}

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }

    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
