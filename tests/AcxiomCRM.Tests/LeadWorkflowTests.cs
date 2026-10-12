using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests;

public class LeadWorkflowTests
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
    public void Opportunity_WeightedAmount_CalculatesCorrectly()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Cloud Deal",
            Amount = 100000m,
            Probability = 75,
            Stage = "Negotiation"
        };

        // Formula: Amount * Probability / 100
        Assert.Equal(75000m, opp.WeightedAmount);
    }

    [Fact]
    public async Task Lead_Conversion_CreatesCustomerAndOpportunity()
    {
        using var db = CreateInMemoryDbContext();

        var lead = new Lead
        {
            LeadCode = "LEAD-0001",
            LeadName = "Global Tech Logistics",
            Email = "contact@globaltech.com",
            Phone = "+91 9988776655",
            CompanyName = "Global Tech",
            Status = "Qualified",
            Priority = "High",
            ExpectedValue = 50000m,
            CreatedDate = DateTime.UtcNow
        };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        // Perform Conversion Logic
        var customer = new Customer
        {
            CustomerCode = "CUST-0001",
            CustomerName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Status = "Active",
            CreatedDate = DateTime.UtcNow
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var opp = new Opportunity
        {
            OpportunityName = $"{lead.LeadName} - Deal",
            CustomerId = customer.CustomerId,
            LeadId = lead.LeadId,
            Amount = lead.ExpectedValue,
            Stage = "Qualification",
            Probability = 30,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            Status = "Open",
            CreatedDate = DateTime.UtcNow
        };
        db.Opportunities.Add(opp);
        await db.SaveChangesAsync();

        lead.Status = "Converted";
        lead.ConvertedCustomerId = customer.CustomerId;
        lead.ConvertedOpportunityId = opp.OpportunityId;
        db.Leads.Update(lead);
        await db.SaveChangesAsync();

        // Verify state
        var updatedLead = await db.Leads.FindAsync(lead.LeadId);
        Assert.NotNull(updatedLead);
        Assert.Equal("Converted", updatedLead.Status);
        Assert.Equal(customer.CustomerId, updatedLead.ConvertedCustomerId);
        Assert.Equal(opp.OpportunityId, updatedLead.ConvertedOpportunityId);

        var createdCustomer = await db.Customers.FindAsync(customer.CustomerId);
        Assert.NotNull(createdCustomer);
        Assert.Equal("Global Tech Logistics", createdCustomer.CustomerName);
    }
}
