using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new List<string>() };
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateCustomerDto
{
    [Required]
    [StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public string? OwnerId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCustomerDto : CreateCustomerDto
{
}
