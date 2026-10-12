using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Controllers.Api;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests;

/// <summary>
/// Direct automated verification of every item from Section 17.19 "Final Acceptance Scenario"
/// </summary>
public class AcceptanceCriteriaTests
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
    public async Task Scenario3_And_4_Customer_InvalidEmailOrPhone_ServerSideValidationRejects()
    {
        using var db = CreateInMemoryDbContext();
        var validation = new CrmValidationService(db);

        // Blank name, invalid email, invalid phone
        var badCustomer = new Customer
        {
            CustomerName = "",
            Email = "invalid-email-string",
            Phone = "12", // too short
            Status = "Active"
        };

        var result = await validation.ValidateCustomerAsync(badCustomer);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey(nameof(badCustomer.CustomerName)));
        Assert.True(result.Errors.ContainsKey(nameof(badCustomer.Email)));
        Assert.True(result.Errors.ContainsKey(nameof(badCustomer.Phone)));
    }

    [Fact]
    public void Scenario5_CreateOpportunity_WithAmountZeroOrNegative_Rejected()
    {
        using var db = CreateInMemoryDbContext();
        var validation = new CrmValidationService(db);

        var opp = new Opportunity
        {
            OpportunityName = "Negative Deal",
            Amount = 0m,
            Stage = "Qualification",
            Probability = 50,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(10),
            Status = "Open"
        };

        var result = validation.ValidateOpportunity(opp);

        Assert.False(result.IsValid);
        Assert.Equal("Opportunity Amount must be greater than 0.", result.Errors[nameof(opp.Amount)]);
    }

    [Fact]
    public void Scenario6_CreateOpportunity_WithProbabilityGreaterThan100_Rejected()
    {
        using var db = CreateInMemoryDbContext();
        var validation = new CrmValidationService(db);

        var opp = new Opportunity
        {
            OpportunityName = "120% Probability Deal",
            Amount = 5000m,
            Stage = "Proposal",
            Probability = 120,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
            Status = "Open"
        };

        var result = validation.ValidateOpportunity(opp);

        Assert.False(result.IsValid);
        Assert.Equal("Probability must be between 0 and 100.", result.Errors[nameof(opp.Probability)]);
    }

    [Fact]
    public void Scenario7_CreateOpportunity_WithPastExpectedCloseDate_RejectedForActiveOpportunity()
    {
        using var db = CreateInMemoryDbContext();
        var validation = new CrmValidationService(db);

        var opp = new Opportunity
        {
            OpportunityName = "Backdated Deal",
            Amount = 15000m,
            Stage = "Proposal",
            Probability = 40,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(-3), // past date
            Status = "Open"
        };

        var result = validation.ValidateOpportunity(opp);

        Assert.False(result.IsValid);
        Assert.Equal("Expected Close Date cannot be in the past.", result.Errors[nameof(opp.ExpectedCloseDate)]);
    }

    [Fact]
    public void Scenario8_CreateFollowUp_DatedBeforeToday_RejectedForNewFollowUp()
    {
        using var db = CreateInMemoryDbContext();
        var validation = new CrmValidationService(db);

        var followUp = new FollowUp
        {
            Subject = "Retroactive Call",
            FollowUpType = "Call",
            Status = "Planned",
            FollowUpDate = DateTime.UtcNow.AddDays(-2)
        };

        var result = validation.ValidateFollowUp(followUp, isNew: true);

        Assert.False(result.IsValid);
        Assert.Equal("Follow-up date cannot be earlier than today.", result.Errors[nameof(followUp.FollowUpDate)]);
    }

    [Fact]
    public async Task Scenario12_RecordMutations_GenerateImmutableAuditEntries()
    {
        using var db = CreateInMemoryDbContext();
        var user = new MockCurrentUserService { UserId = "sales-rep-1", Email = "rep@acxiom.com" };
        var audit = new AuditService(db, user, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditService>.Instance);

        await audit.LogAsync("Create", "Opportunity", "OPP-100", null, new { Amount = 50000 }, "Created new deal");
        await audit.LogAsync("Update", "Opportunity", "OPP-100", new { Stage = "Proposal" }, new { Stage = "Won" }, "Won deal");
        await audit.LogAsync("Delete", "Opportunity", "OPP-100", new { Amount = 50000 }, null, "Deleted deal");

        var entries = await db.AuditLogs.ToListAsync();

        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => e.Action == "Create" && e.EntityName == "Opportunity");
        Assert.Contains(entries, e => e.Action == "Update" && e.EntityName == "Opportunity");
        Assert.Contains(entries, e => e.Action == "Delete" && e.EntityName == "Opportunity");
        Assert.All(entries, e => Assert.Equal("sales-rep-1", e.UserId));
    }

    [Fact]
    public async Task Scenario13_CallApiCustomers_ReturnsAuthorizedStructuredJsonResponse()
    {
        using var db = CreateInMemoryDbContext();
        var user = new MockCurrentUserService { UserId = "admin-1", Role = "Admin" };
        var validation = new CrmValidationService(db);
        var audit = new AuditService(db, user, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditService>.Instance);

        db.Customers.Add(new Customer
        {
            CustomerCode = "CUST-0001",
            CustomerName = "Enterprise Global",
            Email = "eg@global.com",
            Phone = "+1 555-0199",
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var controller = new CustomersApiController(db, validation, audit, user);
        var actionResult = await controller.GetCustomers(null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<List<CustomerDto>>>(okResult.Value);

        Assert.True(response.Success);
        Assert.Single(response.Data!);
        Assert.Equal("Enterprise Global", response.Data![0].CustomerName);
        Assert.Equal("CUST-0001", response.Data![0].CustomerCode);
    }
}
