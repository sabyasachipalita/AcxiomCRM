using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class UserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class UserListViewModel
{
    public List<UserItemViewModel> Users { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "SalesExecutive";

    public bool IsActive { get; set; } = true;

    public bool IsLockedOut { get; set; }

    public SelectList? RolesList { get; set; }
}

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
