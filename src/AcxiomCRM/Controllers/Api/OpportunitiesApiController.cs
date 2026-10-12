using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Produces("application/json")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICrmValidationService _validationService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public OpportunitiesApiController(
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
    public async Task<ActionResult<ApiResponse<List<OpportunityDto>>>> GetOpportunities([FromQuery] string? stage, [FromQuery] string? status)
    {
        var query = _db.Opportunities.Include(o => o.Customer).Include(o => o.Owner).AsNoTracking().AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(o => o.AssignedTo == _currentUserService.UserId);
        }

        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(o => o.Stage == stage);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);

        var list = await query.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            AssignedToName = o.Owner != null ? o.Owner.FullName : null,
            CreatedDate = o.CreatedDate
        }).ToListAsync();

        return Ok(ApiResponse<List<OpportunityDto>>.Ok(list, $"Retrieved {list.Count} opportunities"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OpportunityDto>>> GetOpportunity(int id)
    {
        var o = await _db.Opportunities.Include(x => x.Customer).Include(x => x.Owner).FirstOrDefaultAsync(x => x.OpportunityId == id);
        if (o == null)
        {
            return NotFound(ApiResponse<OpportunityDto>.Fail($"Opportunity with ID {id} was not found."));
        }

        if (_currentUserService.IsSalesExecutive && o.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var dto = new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            AssignedToName = o.Owner != null ? o.Owner.FullName : null,
            CreatedDate = o.CreatedDate
        };

        return Ok(ApiResponse<OpportunityDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OpportunityDto>>> CreateOpportunity([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<OpportunityDto>.Fail("Invalid opportunity data.", errors));
        }

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status,
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : dto.AssignedTo,
            Notes = dto.Notes,
            CreatedDate = DateTime.UtcNow
        };

        var validation = _validationService.ValidateOpportunity(opp);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<OpportunityDto>.Fail("Business validation failed.", validation.Errors.Values.ToList()));
        }

        _db.Opportunities.Add(opp);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Opportunity", opp.OpportunityId.ToString(),
            null, new { opp.OpportunityName, opp.Amount, opp.Stage },
            $"API Created Opportunity '{opp.OpportunityName}'");

        var resultDto = new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            CreatedDate = opp.CreatedDate
        };

        return CreatedAtAction(nameof(GetOpportunity), new { id = opp.OpportunityId }, ApiResponse<OpportunityDto>.Ok(resultDto, "Opportunity created"));
    }
}
