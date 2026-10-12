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
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivitiesController(
        ApplicationDbContext db,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? typeFilter, string? statusFilter, string? assignedFilter)
    {
        var query = _db.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Owner)
            .AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(a => a.AssignedTo == _currentUserService.UserId);
        }
        else if (!string.IsNullOrEmpty(assignedFilter))
        {
            query = query.Where(a => a.AssignedTo == assignedFilter);
        }

        if (!string.IsNullOrEmpty(typeFilter))
        {
            query = query.Where(a => a.ActivityType == typeFilter);
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(a => a.Status == statusFilter);
        }

        var activities = await query
            .OrderByDescending(a => a.ActivityDate)
            .ToListAsync();

        var salesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        var ownersList = new SelectList(salesUsers, "Id", "FullName", assignedFilter);

        var model = new ActivityListViewModel
        {
            Activities = activities,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter,
            AssignedFilter = assignedFilter,
            OwnersList = ownersList
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        var model = new ActivityFormViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow,
            ActivityType = "Call",
            Status = "Completed",
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : null,
            CustomersList = await GetCustomersListAsync(customerId),
            LeadsList = await GetLeadsListAsync(leadId),
            OwnersList = await GetSalesUsersListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CustomersList = await GetCustomersListAsync(model.CustomerId);
            model.LeadsList = await GetLeadsListAsync(model.LeadId);
            model.OwnersList = await GetSalesUsersListAsync(model.AssignedTo);
            return View(model);
        }

        var activity = new Activity
        {
            ActivityType = model.ActivityType,
            Subject = model.Subject,
            Description = model.Description,
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : model.AssignedTo,
            Status = model.Status,
            CreatedDate = DateTime.UtcNow
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Activity", activity.ActivityId.ToString(),
            null, new { activity.ActivityType, activity.Subject, activity.ActivityDate },
            $"Logged {activity.ActivityType}: '{activity.Subject}'");

        TempData["SuccessMessage"] = "Activity logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && activity.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        _db.Activities.Remove(activity);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Activity", id.ToString(),
            new { activity.ActivityId, activity.Subject }, null, $"Deleted Activity '{activity.Subject}'");

        TempData["SuccessMessage"] = "Activity deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SelectList> GetCustomersListAsync(int? selectedId = null)
    {
        var customers = await _db.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        return new SelectList(customers, "CustomerId", "CustomerName", selectedId);
    }

    private async Task<SelectList> GetLeadsListAsync(int? selectedId = null)
    {
        var leads = await _db.Leads.OrderBy(l => l.LeadName).ToListAsync();
        return new SelectList(leads, "LeadId", "LeadName", selectedId);
    }

    private async Task<SelectList> GetSalesUsersListAsync(string? selectedId = null)
    {
        var users = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        return new SelectList(users, "Id", "FullName", selectedId);
    }
}
