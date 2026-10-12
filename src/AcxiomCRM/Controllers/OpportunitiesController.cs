using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICrmValidationService _validationService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunitiesController(
        ApplicationDbContext db,
        ICurrentUserService currentUserService,
        ICrmValidationService validationService,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _currentUserService = currentUserService;
        _validationService = validationService;
        _auditService = auditService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? stageFilter, string? statusFilter, string? assignedFilter, int page = 1)
    {
        const int pageSize = 10;
        var query = _db.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.Owner)
            .AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(o => o.AssignedTo == _currentUserService.UserId);
        }
        else if (!string.IsNullOrEmpty(assignedFilter))
        {
            query = query.Where(o => o.AssignedTo == assignedFilter);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(o =>
                o.OpportunityName.ToLower().Contains(term) ||
                (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(stageFilter))
        {
            query = query.Where(o => o.Stage == stageFilter);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(o => o.Status == statusFilter);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var opportunities = await query
            .OrderByDescending(o => o.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var openOpps = await query.Where(o => o.Status == "Open").Select(o => new { o.Amount, o.Probability }).ToListAsync();
        var totalPipeline = openOpps.Sum(o => o.Amount);
        var weightedPipeline = openOpps.Sum(o => Math.Round(o.Amount * o.Probability / 100m, 2));

        var salesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        var ownersList = new SelectList(salesUsers, "Id", "FullName", assignedFilter);

        var model = new OpportunityListViewModel
        {
            Opportunities = opportunities,
            SearchTerm = searchTerm,
            StageFilter = stageFilter,
            StatusFilter = statusFilter,
            AssignedFilter = assignedFilter,
            OwnersList = ownersList,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            TotalPipelineValue = totalPipeline,
            WeightedPipelineValue = weightedPipeline
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var opp = await _db.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.Owner)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opp == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && opp.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var followUps = await _db.FollowUps
            .Where(f => f.OpportunityId == id)
            .OrderByDescending(f => f.FollowUpDate)
            .ToListAsync();

        var model = new OpportunityDetailsViewModel
        {
            Opportunity = opp,
            FollowUps = followUps
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        var model = new OpportunityFormViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            Stage = "Qualification",
            Probability = 20,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            Status = "Open",
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : null,
            CustomersList = await GetCustomersListAsync(customerId),
            LeadsList = await GetLeadsListAsync(leadId),
            OwnersList = await GetSalesUsersListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityFormViewModel model)
    {
        var opp = new Opportunity
        {
            OpportunityName = model.OpportunityName,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = model.Status,
            Notes = model.Notes,
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : model.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        // Business Validation
        var validation = _validationService.ValidateOpportunity(opp);
        foreach (var err in validation.Errors)
        {
            ModelState.AddModelError(err.Key, err.Value);
        }

        if (!ModelState.IsValid)
        {
            model.CustomersList = await GetCustomersListAsync(model.CustomerId);
            model.LeadsList = await GetLeadsListAsync(model.LeadId);
            model.OwnersList = await GetSalesUsersListAsync(model.AssignedTo);
            return View(model);
        }

        _db.Opportunities.Add(opp);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Opportunity", opp.OpportunityId.ToString(),
            null, new { opp.OpportunityName, opp.Amount, opp.Stage, opp.Probability },
            $"Created Opportunity '{opp.OpportunityName}' with Amount ${opp.Amount}");

        TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opp = await _db.Opportunities.FindAsync(id);
        if (opp == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && opp.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var model = new OpportunityFormViewModel
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo,
            Notes = opp.Notes,
            CustomersList = await GetCustomersListAsync(opp.CustomerId),
            LeadsList = await GetLeadsListAsync(opp.LeadId),
            OwnersList = await GetSalesUsersListAsync(opp.AssignedTo)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityFormViewModel model)
    {
        if (id != model.OpportunityId) return BadRequest();

        var opp = await _db.Opportunities.FindAsync(id);
        if (opp == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && opp.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var oldSnapshot = new
        {
            opp.OpportunityName,
            opp.Amount,
            opp.Stage,
            opp.Probability,
            opp.ExpectedCloseDate,
            opp.Status
        };

        opp.OpportunityName = model.OpportunityName;
        opp.CustomerId = model.CustomerId;
        opp.LeadId = model.LeadId;
        opp.Amount = model.Amount;
        opp.Stage = model.Stage;
        opp.Probability = model.Probability;
        opp.ExpectedCloseDate = model.ExpectedCloseDate;
        opp.Status = model.Status;
        opp.Notes = model.Notes;

        // Auto sync status if stage changes to Won or Lost
        if (opp.Stage == "Won") opp.Status = "Won";
        else if (opp.Stage == "Lost") opp.Status = "Lost";

        if (!_currentUserService.IsSalesExecutive)
        {
            opp.AssignedTo = model.AssignedTo;
        }

        var validation = _validationService.ValidateOpportunity(opp);
        foreach (var err in validation.Errors)
        {
            ModelState.AddModelError(err.Key, err.Value);
        }

        if (!ModelState.IsValid)
        {
            model.CustomersList = await GetCustomersListAsync(model.CustomerId);
            model.LeadsList = await GetLeadsListAsync(model.LeadId);
            model.OwnersList = await GetSalesUsersListAsync(model.AssignedTo);
            return View(model);
        }

        _db.Opportunities.Update(opp);
        await _db.SaveChangesAsync();

        var newSnapshot = new
        {
            opp.OpportunityName,
            opp.Amount,
            opp.Stage,
            opp.Probability,
            opp.ExpectedCloseDate,
            opp.Status
        };

        await _auditService.LogAsync("Update", "Opportunity", opp.OpportunityId.ToString(),
            oldSnapshot, newSnapshot, $"Updated Opportunity '{opp.OpportunityName}' (Stage: {opp.Stage})");

        TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var opp = await _db.Opportunities.FindAsync(id);
        if (opp == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && opp.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var snapshot = new { opp.OpportunityId, opp.OpportunityName, opp.Amount };

        _db.Opportunities.Remove(opp);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Opportunity", id.ToString(),
            snapshot, null, $"Deleted Opportunity '{opp.OpportunityName}'");

        TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' removed successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SelectList> GetCustomersListAsync(int? selectedId = null)
    {
        var query = _db.Customers.AsQueryable();
        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(c => c.OwnerId == _currentUserService.UserId);
        }
        var customers = await query.OrderBy(c => c.CustomerName).ToListAsync();
        return new SelectList(customers, "CustomerId", "CustomerName", selectedId);
    }

    private async Task<SelectList> GetLeadsListAsync(int? selectedId = null)
    {
        var query = _db.Leads.AsQueryable();
        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(l => l.AssignedTo == _currentUserService.UserId);
        }
        var leads = await query.OrderBy(l => l.LeadName).ToListAsync();
        return new SelectList(leads, "LeadId", "LeadName", selectedId);
    }

    private async Task<SelectList> GetSalesUsersListAsync(string? selectedId = null)
    {
        var users = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        return new SelectList(users, "Id", "FullName", selectedId);
    }
}
