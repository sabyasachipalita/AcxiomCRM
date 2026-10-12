using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests;

public class MockCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "user-123";
    public string? UserName { get; set; } = "tester@acxiomcrm.local";
    public string? Email { get; set; } = "tester@acxiomcrm.local";
    public string? Role { get; set; } = "Admin";
    public bool IsAuthenticated => true;
    public bool IsAdmin => Role == "Admin";
    public bool IsManager => Role == "Manager";
    public bool IsSalesExecutive => Role == "SalesExecutive";
    public string? IpAddress => "192.168.1.50";
}

public class AuditLogTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AuditService_RecordsActionsCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var currentUserService = new MockCurrentUserService();
        var auditService = new AuditService(db, currentUserService, NullLogger<AuditService>.Instance);

        // 1. Log Create
        await auditService.LogAsync("Create", "Customer", "101",
            null, new { CustomerName = "Acme Corp" }, "Customer created");

        // 2. Log Update
        await auditService.LogAsync("Update", "Customer", "101",
            new { CustomerName = "Acme Corp" }, new { CustomerName = "Acme Corporation" }, "Customer name changed");

        // 3. Log Delete
        await auditService.LogAsync("Delete", "Customer", "101",
            new { CustomerName = "Acme Corporation" }, null, "Customer removed");

        var logs = await db.AuditLogs.OrderBy(l => l.AuditLogId).ToListAsync();

        Assert.Equal(3, logs.Count);
        Assert.Equal("Create", logs[0].Action);
        Assert.Equal("Update", logs[1].Action);
        Assert.Equal("Delete", logs[2].Action);
        Assert.Equal("Customer", logs[0].EntityName);
        Assert.Equal("user-123", logs[0].UserId);
        Assert.Equal("192.168.1.50", logs[0].IpAddress);
        Assert.Contains("Acme Corporation", logs[1].NewValue);
    }
}
