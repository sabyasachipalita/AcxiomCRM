using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [MaxLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a role.")]
    public string Role { get; set; } = "SalesExecutive";
}
