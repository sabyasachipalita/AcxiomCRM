using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    [MaxLength(150)]
    public string? UserName { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security

    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, User, Role, Auth

    [MaxLength(100)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }
}
