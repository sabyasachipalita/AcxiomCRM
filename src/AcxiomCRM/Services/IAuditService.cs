using System.Text.Json;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? recordId, object? oldValue = null, object? newValue = null, string? details = null, string? overrideUserId = null, string? overrideUserName = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext db, ICurrentUserService currentUserService, ILogger<AuditService> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string? recordId,
        object? oldValue = null,
        object? newValue = null,
        string? details = null,
        string? overrideUserId = null,
        string? overrideUserName = null)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var log = new AuditLog
            {
                UserId = overrideUserId ?? _currentUserService.UserId,
                UserName = overrideUserName ?? _currentUserService.UserName ?? _currentUserService.Email,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue is string oldStr ? oldStr : (oldValue != null ? JsonSerializer.Serialize(oldValue, options) : null),
                NewValue = newValue is string newStr ? newStr : (newValue != null ? JsonSerializer.Serialize(newValue, options) : null),
                Details = details,
                CreatedDate = DateTime.UtcNow,
                IpAddress = _currentUserService.IpAddress
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Logging shouldn't crash the main transaction
            _logger.LogError(ex, "Failed to write audit log for {Action} on {EntityName}", action, entityName);
        }
    }
}
