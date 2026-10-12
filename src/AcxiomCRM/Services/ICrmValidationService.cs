using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public Dictionary<string, string> Errors { get; } = new();

    public void AddError(string field, string message)
    {
        Errors[field] = message;
    }
}

public interface ICrmValidationService
{
    Task<ValidationResult> ValidateCustomerAsync(Customer customer, int? existingId = null);
    Task<ValidationResult> ValidateLeadAsync(Lead lead, int? existingId = null, string? currentStatus = null);
    ValidationResult ValidateOpportunity(Opportunity opportunity);
    ValidationResult ValidateFollowUp(FollowUp followUp, bool isNew = false);
}

public class CrmValidationService : ICrmValidationService
{
    private readonly ApplicationDbContext _db;

    public CrmValidationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ValidationResult> ValidateCustomerAsync(Customer customer, int? existingId = null)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(customer.CustomerName))
        {
            result.AddError(nameof(customer.CustomerName), "Customer Name is required.");
        }
        else if (customer.CustomerName.Length > 150)
        {
            result.AddError(nameof(customer.CustomerName), "Customer Name cannot exceed 150 characters.");
        }

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            result.AddError(nameof(customer.Email), "Enter a valid email address.");
        }
        else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(customer.Email))
        {
            result.AddError(nameof(customer.Email), "Enter a valid email address.");
        }
        else
        {
            var emailExists = await _db.Customers
                .AnyAsync(c => c.Email.ToLower() == customer.Email.ToLower() && (!existingId.HasValue || c.CustomerId != existingId.Value));
            if (emailExists)
            {
                result.AddError(nameof(customer.Email), "A customer with this email address already exists.");
            }
        }

        if (string.IsNullOrWhiteSpace(customer.Phone))
        {
            result.AddError(nameof(customer.Phone), "Enter a valid phone number.");
        }
        else
        {
            var phoneClean = customer.Phone.Trim();
            if (phoneClean.Length < 7 || phoneClean.Length > 20)
            {
                result.AddError(nameof(customer.Phone), "Enter a valid phone number (7-20 digits).");
            }
            else
            {
                var phoneExists = await _db.Customers
                    .AnyAsync(c => c.Phone == customer.Phone && (!existingId.HasValue || c.CustomerId != existingId.Value));
                if (phoneExists)
                {
                    result.AddError(nameof(customer.Phone), "A customer with this phone number already exists.");
                }
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateLeadAsync(Lead lead, int? existingId = null, string? currentStatus = null)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(lead.LeadName))
        {
            result.AddError(nameof(lead.LeadName), "Lead name is mandatory.");
        }

        if (string.IsNullOrWhiteSpace(lead.Email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(lead.Email))
        {
            result.AddError(nameof(lead.Email), "Enter a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(lead.Phone))
        {
            result.AddError(nameof(lead.Phone), "Enter a valid phone number.");
        }

        if (lead.ExpectedValue < 0)
        {
            result.AddError(nameof(lead.ExpectedValue), "Expected value cannot be negative.");
        }

        var validStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
        if (!validStatuses.Contains(lead.Status))
        {
            result.AddError(nameof(lead.Status), $"Invalid lead status: {lead.Status}");
        }

        // Workflow transition check
        if (!string.IsNullOrEmpty(currentStatus) && currentStatus != lead.Status)
        {
            if (currentStatus == "Converted" && lead.Status != "Converted")
            {
                result.AddError(nameof(lead.Status), "Converted leads cannot change status directly.");
            }
        }

        return await Task.FromResult(result);
    }

    public ValidationResult ValidateOpportunity(Opportunity opportunity)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
        {
            result.AddError(nameof(opportunity.OpportunityName), "Opportunity Name is required.");
        }

        // Rule: Amount > 0
        if (opportunity.Amount <= 0)
        {
            result.AddError(nameof(opportunity.Amount), "Opportunity Amount must be greater than 0.");
        }

        // Rule: Probability 0 to 100
        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            result.AddError(nameof(opportunity.Probability), "Probability must be between 0 and 100.");
        }

        // Rule: Expected Close Date cannot be in the past for an active Opportunity
        if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            result.AddError(nameof(opportunity.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        }

        var validStages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        if (!validStages.Contains(opportunity.Stage))
        {
            result.AddError(nameof(opportunity.Stage), "Invalid opportunity pipeline stage.");
        }

        return result;
    }

    public ValidationResult ValidateFollowUp(FollowUp followUp, bool isNew = false)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(followUp.Subject))
        {
            result.AddError(nameof(followUp.Subject), "Subject is required.");
        }

        // Rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if ((isNew || followUp.Status == "Planned") && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            result.AddError(nameof(followUp.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        var validStatuses = new[] { "Planned", "Completed", "Missed", "Cancelled" };
        if (!validStatuses.Contains(followUp.Status))
        {
            result.AddError(nameof(followUp.Status), "Invalid follow-up status.");
        }

        return result;
    }
}
