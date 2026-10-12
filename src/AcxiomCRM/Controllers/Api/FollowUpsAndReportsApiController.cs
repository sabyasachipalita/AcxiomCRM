using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Produces("application/json")]
public class FollowUpsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICrmValidationService _validationService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public FollowUpsApiController(
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
    public async Task<ActionResult<ApiResponse<List<FollowUpDto>>>> GetFollowUps([FromQuery] string? status)
    {
        var query = _db.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.Owner).AsNoTracking().AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(f => f.AssignedTo == _currentUserService.UserId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        var list = await query.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
            LeadId = f.LeadId,
            LeadName = f.Lead != null ? f.Lead.LeadName : null,
            OpportunityId = f.OpportunityId,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedToName = f.Owner != null ? f.Owner.FullName : null
        }).ToListAsync();

        return Ok(ApiResponse<List<FollowUpDto>>.Ok(list, $"Retrieved {list.Count} follow-ups"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FollowUpDto>>> CreateFollowUp([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<FollowUpDto>.Fail("Invalid follow-up data.", errors));
        }

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Subject = dto.Subject,
            Remarks = dto.Remarks,
            Status = dto.Status,
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : dto.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        var validation = _validationService.ValidateFollowUp(followUp, isNew: true);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<FollowUpDto>.Fail("Business validation failed.", validation.Errors.Values.ToList()));
        }

        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "FollowUp", followUp.FollowUpId.ToString(),
            null, new { followUp.Subject, followUp.FollowUpDate },
            $"API Created Follow-Up '{followUp.Subject}'");

        var resultDto = new FollowUpDto
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status
        };

        return Ok(ApiResponse<FollowUpDto>.Ok(resultDto, "Follow-up created successfully"));
    }
}

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public ReportsApiController(ApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    [HttpGet("pipeline")]
    public async Task<ActionResult<ApiResponse<object>>> GetPipelineReport()
    {
        var oppQuery = _db.Opportunities.Include(o => o.Owner).AsNoTracking().AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            oppQuery = oppQuery.Where(o => o.AssignedTo == _currentUserService.UserId);
        }

        var opps = await oppQuery.ToListAsync();

        var stageSummary = opps
            .GroupBy(o => o.Stage)
            .Select(g => new
            {
                Stage = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedAmount)
            });

        var ownerSummary = opps
            .GroupBy(o => o.Owner?.FullName ?? "Unassigned")
            .Select(g => new
            {
                Owner = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedAmount)
            });

        return Ok(ApiResponse<object>.Ok(new
        {
            TotalAmount = opps.Sum(o => o.Amount),
            WeightedAmount = opps.Sum(o => o.WeightedAmount),
            StageSummary = stageSummary,
            OwnerSummary = ownerSummary
        }, "Pipeline report generated"));
    }
}
