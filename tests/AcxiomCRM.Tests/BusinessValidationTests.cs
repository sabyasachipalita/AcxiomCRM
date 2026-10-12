using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests;

public class BusinessValidationTests
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

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Opportunity_AmountLessOrEqualZero_ShouldBeRejected(decimal amount)
    {
        using var db = CreateInMemoryDbContext();
        var service = new CrmValidationService(db);

        var opp = new Opportunity
        {
            OpportunityName = "Big Deal",
            Amount = amount,
            Stage = "Qualification",
            Probability = 20,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            Status = "Open"
        };

        var result = service.ValidateOpportunity(opp);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey(nameof(opp.Amount)));
        Assert.Equal("Opportunity Amount must be greater than 0.", result.Errors[nameof(opp.Amount)]);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(-5)]
    public void Opportunity_ProbabilityOutOfRange_ShouldBeRejected(int probability)
    {
        using var db = CreateInMemoryDbContext();
        var service = new CrmValidationService(db);

        var opp = new Opportunity
        {
            OpportunityName = "Big Deal",
            Amount = 15000,
            Stage = "Proposal",
            Probability = probability,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            Status = "Open"
        };

        var result = service.ValidateOpportunity(opp);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey(nameof(opp.Probability)));
        Assert.Equal("Probability must be between 0 and 100.", result.Errors[nameof(opp.Probability)]);
    }

    [Fact]
    public void Opportunity_PastCloseDate_WhenActive_ShouldBeRejected()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CrmValidationService(db);

        var opp = new Opportunity
        {
            OpportunityName = "Old Deal",
            Amount = 25000,
            Stage = "Negotiation",
            Probability = 50,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(-2), // Yesterday
            Status = "Open"
        };

        var result = service.ValidateOpportunity(opp);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey(nameof(opp.ExpectedCloseDate)));
        Assert.Equal("Expected Close Date cannot be in the past.", result.Errors[nameof(opp.ExpectedCloseDate)]);
    }

    [Fact]
    public void FollowUp_DateEarlierThanToday_ShouldBeRejected()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CrmValidationService(db);

        var followUp = new FollowUp
        {
            Subject = "Customer Check-in",
            FollowUpType = "Call",
            Status = "Planned",
            FollowUpDate = DateTime.UtcNow.AddDays(-1) // yesterday
        };

        var result = service.ValidateFollowUp(followUp, isNew: true);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey(nameof(followUp.FollowUpDate)));
        Assert.Equal("Follow-up date cannot be earlier than today.", result.Errors[nameof(followUp.FollowUpDate)]);
    }

    [Fact]
    public async Task Customer_DuplicateEmail_ShouldBeRejected()
    {
        using var db = CreateInMemoryDbContext();
        db.Customers.Add(new Customer
        {
            CustomerCode = "CUST-0001",
            CustomerName = "First Customer",
            Email = "unique@client.com",
            Phone = "+1234567890",
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var service = new CrmValidationService(db);

        var newCustomer = new Customer
        {
            CustomerName = "Second Customer",
            Email = "unique@client.com", // Duplicate
            Phone = "+9876543210",
            Status = "Active"
        };

        var result = await service.ValidateCustomerAsync(newCustomer);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey(nameof(newCustomer.Email)));
        Assert.Equal("A customer with this email address already exists.", result.Errors[nameof(newCustomer.Email)]);
    }

    [Fact]
    public async Task Customer_DuplicatePhone_ShouldBeRejected()
    {
        using var db = CreateInMemoryDbContext();
        db.Customers.Add(new Customer
        {
            CustomerCode = "CUST-0001",
            CustomerName = "First Customer",
            Email = "c1@client.com",
            Phone = "+91 9998887776",
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var service = new CrmValidationService(db);

        var newCustomer = new Customer
        {
            CustomerName = "Second Customer",
            Email = "c2@client.com",
            Phone = "+91 9998887776", // Duplicate
            Status = "Active"
        };

        var result = await service.ValidateCustomerAsync(newCustomer);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey(nameof(newCustomer.Phone)));
        Assert.Equal("A customer with this phone number already exists.", result.Errors[nameof(newCustomer.Phone)]);
    }

    [Fact]
    public async Task Lead_InvalidStatus_ShouldBeRejected()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CrmValidationService(db);

        var lead = new Lead
        {
            LeadName = "Invalid Status Lead",
            Email = "lead@example.com",
            Phone = "1234567890",
            Status = "BogusStatus",
            ExpectedValue = 5000
        };

        var result = await service.ValidateLeadAsync(lead);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey(nameof(lead.Status)));
    }
}
