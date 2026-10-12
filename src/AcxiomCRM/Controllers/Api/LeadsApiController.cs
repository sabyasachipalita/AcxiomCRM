using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Produces("application/json")]
public class LeadsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICrmValidationService _validationService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public LeadsApiController(
        ApplicationDbContext db,
        ICrmValidationService validationService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _validationService = validationService;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LeadDto>>>> GetLeads([FromQuery] string? search, [FromQuery] string? status)
    {
        var query = _db.Leads.Include(l => l.Owner).AsNoTracking().AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(l => l.AssignedTo == _currentUserService.UserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(s) || l.Email.ToLower().Contains(s) || l.CompanyName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var leads = await query.Select(l => new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            AssignedToName = l.Owner != null ? l.Owner.FullName : null,
            CreatedDate = l.CreatedDate,
            ConvertedCustomerId = l.ConvertedCustomerId,
            ConvertedOpportunityId = l.ConvertedOpportunityId
        }).ToListAsync();

        return Ok(ApiResponse<List<LeadDto>>.Ok(leads, $"Retrieved {leads.Count} leads"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LeadDto>>> GetLead(int id)
    {
        var l = await _db.Leads.Include(x => x.Owner).FirstOrDefaultAsync(x => x.LeadId == id);
        if (l == null)
        {
            return NotFound(ApiResponse<LeadDto>.Fail($"Lead with ID {id} was not found."));
        }

        if (_currentUserService.IsSalesExecutive && l.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var dto = new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            AssignedToName = l.Owner != null ? l.Owner.FullName : null,
            CreatedDate = l.CreatedDate,
            ConvertedCustomerId = l.ConvertedCustomerId,
            ConvertedOpportunityId = l.ConvertedOpportunityId
        };

        return Ok(ApiResponse<LeadDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LeadDto>>> CreateLead([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<LeadDto>.Fail("Invalid lead data.", errors));
        }

        var lead = new Lead
        {
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName ?? string.Empty,
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : dto.AssignedTo,
            Notes = dto.Notes,
            CreatedDate = DateTime.UtcNow
        };

        var validation = await _validationService.ValidateLeadAsync(lead);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeadDto>.Fail("Business validation failed.", validation.Errors.Values.ToList()));
        }

        var lastId = await _db.Leads.MaxAsync(l => (int?)l.LeadId) ?? 0;
        lead.LeadCode = $"LEAD-{(lastId + 1):D4}";

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Lead", lead.LeadId.ToString(),
            null, new { lead.LeadCode, lead.LeadName, lead.Email, lead.Status },
            $"API Created Lead '{lead.LeadName}'");

        var resultDto = new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate
        };

        return CreatedAtAction(nameof(GetLead), new { id = lead.LeadId }, ApiResponse<LeadDto>.Ok(resultDto, "Lead created"));
    }

    [HttpPost("{id}/convert")]
    public async Task<ActionResult<ApiResponse<object>>> ConvertLead(int id, [FromBody] ConvertLeadDto dto)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Lead with ID {id} was not found."));
        }

        if (_currentUserService.IsSalesExecutive && lead.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        if (lead.Status == "Converted")
        {
            return BadRequest(ApiResponse<object>.Fail("Lead is already converted."));
        }

        // Find or create customer
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
        if (customer == null)
        {
            var lastCustId = await _db.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
            customer = new Customer
            {
                CustomerCode = $"CUST-{(lastCustId + 1):D4}",
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Address = string.Empty,
                City = string.Empty,
                State = string.Empty,
                Status = "Active",
                Notes = $"API converted from Lead {lead.LeadCode}",
                CreatedBy = _currentUserService.Email ?? "API",
                OwnerId = lead.AssignedTo,
                CreatedDate = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
        }

        Opportunity? opp = null;
        if (dto.CreateOpportunity)
        {
            opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName ?? $"{lead.LeadName} - Deal",
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = dto.OpportunityAmount > 0 ? dto.OpportunityAmount : 1000m,
                Stage = dto.OpportunityStage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate ?? DateTime.UtcNow.AddMonths(1),
                Status = "Open",
                AssignedTo = lead.AssignedTo,
                CreatedDate = DateTime.UtcNow
            };

            var oppValidation = _validationService.ValidateOpportunity(opp);
            if (!oppValidation.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Opportunity validation failed.", oppValidation.Errors.Values.ToList()));
            }

            _db.Opportunities.Add(opp);
            await _db.SaveChangesAsync();
        }

        lead.Status = "Converted";
        lead.ConvertedCustomerId = customer.CustomerId;
        if (opp != null) lead.ConvertedOpportunityId = opp.OpportunityId;

        _db.Leads.Update(lead);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Update", "Lead", lead.LeadId.ToString(),
            new { OldStatus = "Qualified" }, new { NewStatus = "Converted" },
            $"API Converted Lead '{lead.LeadName}' to Customer '{customer.CustomerName}'");

        return Ok(ApiResponse<object>.Ok(new
        {
            LeadId = lead.LeadId,
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            OpportunityId = opp?.OpportunityId
        }, "Lead converted successfully"));
    }
}
