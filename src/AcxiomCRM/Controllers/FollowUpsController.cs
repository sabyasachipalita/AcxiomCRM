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
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICrmValidationService _validationService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpsController(
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

    public async Task<IActionResult> Index(string tab = "all", string? statusFilter = null, string? typeFilter = null, string? assignedFilter = null)
    {
        var query = _db.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.Owner)
            .AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(f => f.AssignedTo == _currentUserService.UserId);
        }
        else if (!string.IsNullOrEmpty(assignedFilter))
        {
            query = query.Where(f => f.AssignedTo == assignedFilter);
        }

        var today = DateTime.UtcNow.Date;

        if (tab == "upcoming")
        {
            query = query.Where(f => f.Status == "Planned" && f.FollowUpDate.Date >= today);
        }
        else if (tab == "overdue")
        {
            query = query.Where(f => f.Status == "Planned" && f.FollowUpDate.Date < today);
        }
        else if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(f => f.Status == statusFilter);
        }

        if (!string.IsNullOrEmpty(typeFilter))
        {
            query = query.Where(f => f.FollowUpType == typeFilter);
        }

        var followUps = await query
            .OrderBy(f => f.FollowUpDate)
            .ToListAsync();

        var salesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        var ownersList = new SelectList(salesUsers, "Id", "FullName", assignedFilter);

        var model = new FollowUpListViewModel
        {
            FollowUps = followUps,
            ActiveTab = tab,
            StatusFilter = statusFilter,
            TypeFilter = typeFilter,
            AssignedFilter = assignedFilter,
            OwnersList = ownersList
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
    {
        var model = new FollowUpFormViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
            FollowUpType = "Call",
            Status = "Planned",
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : null,
            CustomersList = await GetCustomersListAsync(customerId),
            LeadsList = await GetLeadsListAsync(leadId),
            OpportunitiesList = await GetOpportunitiesListAsync(opportunityId),
            OwnersList = await GetSalesUsersListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpFormViewModel model)
    {
        var followUp = new FollowUp
        {
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            OpportunityId = model.OpportunityId,
            FollowUpDate = model.FollowUpDate,
            FollowUpType = model.FollowUpType,
            Subject = model.Subject,
            Remarks = model.Remarks,
            Status = model.Status,
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : model.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        // Business Validation
        var validation = _validationService.ValidateFollowUp(followUp, isNew: true);
        foreach (var err in validation.Errors)
        {
            ModelState.AddModelError(err.Key, err.Value);
        }

        if (!ModelState.IsValid)
        {
            model.CustomersList = await GetCustomersListAsync(model.CustomerId);
            model.LeadsList = await GetLeadsListAsync(model.LeadId);
            model.OpportunitiesList = await GetOpportunitiesListAsync(model.OpportunityId);
            model.OwnersList = await GetSalesUsersListAsync(model.AssignedTo);
            return View(model);
        }

        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "FollowUp", followUp.FollowUpId.ToString(),
            null, new { followUp.Subject, followUp.FollowUpDate, followUp.FollowUpType, followUp.Status },
            $"Scheduled Follow-Up '{followUp.Subject}' on {followUp.FollowUpDate:yyyy-MM-dd}");

        TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var followUp = await _db.FollowUps.FindAsync(id);
        if (followUp == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && followUp.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var model = new FollowUpFormViewModel
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo,
            CustomersList = await GetCustomersListAsync(followUp.CustomerId),
            LeadsList = await GetLeadsListAsync(followUp.LeadId),
            OpportunitiesList = await GetOpportunitiesListAsync(followUp.OpportunityId),
            OwnersList = await GetSalesUsersListAsync(followUp.AssignedTo)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUpFormViewModel model)
    {
        if (id != model.FollowUpId) return BadRequest();

        var followUp = await _db.FollowUps.FindAsync(id);
        if (followUp == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && followUp.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var oldSnapshot = new { followUp.Subject, followUp.FollowUpDate, followUp.Status };

        followUp.CustomerId = model.CustomerId;
        followUp.LeadId = model.LeadId;
        followUp.OpportunityId = model.OpportunityId;
        followUp.FollowUpDate = model.FollowUpDate;
        followUp.FollowUpType = model.FollowUpType;
        followUp.Subject = model.Subject;
        followUp.Remarks = model.Remarks;
        followUp.Status = model.Status;

        if (!_currentUserService.IsSalesExecutive)
        {
            followUp.AssignedTo = model.AssignedTo;
        }

        var validation = _validationService.ValidateFollowUp(followUp, isNew: false);
        foreach (var err in validation.Errors)
        {
            ModelState.AddModelError(err.Key, err.Value);
        }

        if (!ModelState.IsValid)
        {
            model.CustomersList = await GetCustomersListAsync(model.CustomerId);
            model.LeadsList = await GetLeadsListAsync(model.LeadId);
            model.OpportunitiesList = await GetOpportunitiesListAsync(model.OpportunityId);
            model.OwnersList = await GetSalesUsersListAsync(model.AssignedTo);
            return View(model);
        }

        _db.FollowUps.Update(followUp);
        await _db.SaveChangesAsync();

        var newSnapshot = new { followUp.Subject, followUp.FollowUpDate, followUp.Status };

        await _auditService.LogAsync("Update", "FollowUp", followUp.FollowUpId.ToString(),
            oldSnapshot, newSnapshot, $"Updated Follow-Up '{followUp.Subject}' (Status: {followUp.Status})");

        TempData["SuccessMessage"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var followUp = await _db.FollowUps.FindAsync(id);
        if (followUp == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && followUp.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        followUp.Status = "Completed";
        if (!string.IsNullOrEmpty(remarks))
        {
            followUp.Remarks = string.IsNullOrEmpty(followUp.Remarks) ? remarks : $"{followUp.Remarks}\n[Completed Notes]: {remarks}";
        }

        _db.FollowUps.Update(followUp);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Update", "FollowUp", followUp.FollowUpId.ToString(),
            new { OldStatus = "Planned" }, new { NewStatus = "Completed" },
            $"Completed Follow-Up '{followUp.Subject}'");

        TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' marked as completed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var followUp = await _db.FollowUps.FindAsync(id);
        if (followUp == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && followUp.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        _db.FollowUps.Remove(followUp);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "FollowUp", id.ToString(),
            new { followUp.FollowUpId, followUp.Subject }, null, $"Deleted Follow-Up '{followUp.Subject}'");

        TempData["SuccessMessage"] = "Follow-up removed.";
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

    private async Task<SelectList> GetOpportunitiesListAsync(int? selectedId = null)
    {
        var query = _db.Opportunities.AsQueryable();
        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(o => o.AssignedTo == _currentUserService.UserId);
        }
        var opps = await query.OrderBy(o => o.OpportunityName).ToListAsync();
        return new SelectList(opps, "OpportunityId", "OpportunityName", selectedId);
    }

    private async Task<SelectList> GetSalesUsersListAsync(string? selectedId = null)
    {
        var users = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        return new SelectList(users, "Id", "FullName", selectedId);
    }
}
