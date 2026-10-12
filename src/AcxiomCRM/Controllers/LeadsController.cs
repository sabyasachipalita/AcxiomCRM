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
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICrmValidationService _validationService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsController(
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

    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? priorityFilter, string? assignedFilter, int page = 1)
    {
        const int pageSize = 10;
        var query = _db.Leads.Include(l => l.Owner).AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(l => l.AssignedTo == _currentUserService.UserId);
        }
        else if (!string.IsNullOrEmpty(assignedFilter))
        {
            query = query.Where(l => l.AssignedTo == assignedFilter);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(term) ||
                l.Email.ToLower().Contains(term) ||
                l.Phone.Contains(term) ||
                l.LeadCode.ToLower().Contains(term) ||
                l.CompanyName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(l => l.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(priorityFilter))
        {
            query = query.Where(l => l.Priority == priorityFilter);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var leads = await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var salesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        var ownersList = new SelectList(salesUsers, "Id", "FullName", assignedFilter);

        var model = new LeadListViewModel
        {
            Leads = leads,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            PriorityFilter = priorityFilter,
            AssignedFilter = assignedFilter,
            OwnersList = ownersList,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lead = await _db.Leads
            .Include(l => l.Owner)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.ConvertedOpportunity)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && lead.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var followUps = await _db.FollowUps
            .Where(f => f.LeadId == id)
            .OrderByDescending(f => f.FollowUpDate)
            .ToListAsync();

        var activities = await _db.Activities
            .Where(a => a.LeadId == id)
            .OrderByDescending(a => a.ActivityDate)
            .ToListAsync();

        var model = new LeadDetailsViewModel
        {
            Lead = lead,
            FollowUps = followUps,
            Activities = activities
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new LeadFormViewModel
        {
            Status = "New",
            Priority = "Medium",
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : null,
            OwnersList = await GetSalesUsersListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadFormViewModel model)
    {
        var lead = new Lead
        {
            LeadName = model.LeadName,
            Email = model.Email,
            Phone = model.Phone,
            CompanyName = model.CompanyName ?? string.Empty,
            Source = model.Source,
            Status = model.Status,
            Priority = model.Priority,
            ExpectedValue = model.ExpectedValue,
            Notes = model.Notes,
            AssignedTo = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : model.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        var validation = await _validationService.ValidateLeadAsync(lead);
        foreach (var err in validation.Errors)
        {
            ModelState.AddModelError(err.Key, err.Value);
        }

        if (!ModelState.IsValid)
        {
            model.OwnersList = await GetSalesUsersListAsync(model.AssignedTo);
            return View(model);
        }

        var lastId = await _db.Leads.MaxAsync(l => (int?)l.LeadId) ?? 0;
        lead.LeadCode = $"LEAD-{(lastId + 1):D4}";

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Lead", lead.LeadId.ToString(),
            null, new { lead.LeadCode, lead.LeadName, lead.Email, lead.Status },
            $"Created Lead '{lead.LeadName}' ({lead.LeadCode})");

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && lead.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var model = new LeadFormViewModel
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
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes,
            OwnersList = await GetSalesUsersListAsync(lead.AssignedTo)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadFormViewModel model)
    {
        if (id != model.LeadId) return BadRequest();

        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && lead.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var oldSnapshot = new
        {
            lead.LeadName,
            lead.Email,
            lead.Status,
            lead.AssignedTo,
            lead.ExpectedValue
        };

        var currentStatus = lead.Status;

        lead.LeadName = model.LeadName;
        lead.Email = model.Email;
        lead.Phone = model.Phone;
        lead.CompanyName = model.CompanyName ?? string.Empty;
        lead.Source = model.Source;
        lead.Status = model.Status;
        lead.Priority = model.Priority;
        lead.ExpectedValue = model.ExpectedValue;
        lead.Notes = model.Notes;

        if (!_currentUserService.IsSalesExecutive)
        {
            lead.AssignedTo = model.AssignedTo;
        }

        var validation = await _validationService.ValidateLeadAsync(lead, id, currentStatus);
        foreach (var err in validation.Errors)
        {
            ModelState.AddModelError(err.Key, err.Value);
        }

        if (!ModelState.IsValid)
        {
            model.OwnersList = await GetSalesUsersListAsync(model.AssignedTo);
            return View(model);
        }

        _db.Leads.Update(lead);
        await _db.SaveChangesAsync();

        var newSnapshot = new
        {
            lead.LeadName,
            lead.Email,
            lead.Status,
            lead.AssignedTo,
            lead.ExpectedValue
        };

        await _auditService.LogAsync("Update", "Lead", lead.LeadId.ToString(),
            oldSnapshot, newSnapshot, $"Updated Lead '{lead.LeadName}' (Status: {lead.Status})");

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && lead.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        if (lead.Status == "Converted")
        {
            TempData["ErrorMessage"] = "This lead is already converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            CompanyName = lead.CompanyName,
            Email = lead.Email,
            Phone = lead.Phone,
            CreateOpportunity = true,
            OpportunityName = $"{lead.LeadName} - Deal",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m,
            OpportunityStage = "Qualification",
            Probability = 30,
            ExpectedCloseDate = DateTime.UtcNow.AddMonths(1),
            AssignedTo = lead.AssignedTo,
            OwnersList = await GetSalesUsersListAsync(lead.AssignedTo)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertViewModel model)
    {
        var lead = await _db.Leads.FindAsync(model.LeadId);
        if (lead == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && lead.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        // 1. Create or Find Customer
        var existingCust = await _db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
        Customer customer;
        if (existingCust != null)
        {
            customer = existingCust;
        }
        else
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
                Notes = $"Converted from Lead {lead.LeadCode}. " + lead.Notes,
                CreatedBy = _currentUserService.Email ?? "System",
                OwnerId = model.AssignedTo ?? lead.AssignedTo,
                CreatedDate = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
        }

        Opportunity? opp = null;
        if (model.CreateOpportunity)
        {
            opp = new Opportunity
            {
                OpportunityName = model.OpportunityName ?? $"{lead.LeadName} - Deal",
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = model.OpportunityAmount > 0 ? model.OpportunityAmount : 1000m,
                Stage = model.OpportunityStage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = "Open",
                AssignedTo = model.AssignedTo ?? lead.AssignedTo,
                CreatedDate = DateTime.UtcNow,
                Notes = $"Originated from converted Lead {lead.LeadCode}"
            };

            var oppValidation = _validationService.ValidateOpportunity(opp);
            if (!oppValidation.IsValid)
            {
                foreach (var err in oppValidation.Errors)
                {
                    ModelState.AddModelError(err.Key, err.Value);
                }
                model.OwnersList = await GetSalesUsersListAsync(model.AssignedTo);
                return View(model);
            }

            _db.Opportunities.Add(opp);
            await _db.SaveChangesAsync();
        }

        // Update Lead state to Converted
        lead.Status = "Converted";
        lead.ConvertedCustomerId = customer.CustomerId;
        if (opp != null) lead.ConvertedOpportunityId = opp.OpportunityId;

        _db.Leads.Update(lead);
        await _db.SaveChangesAsync();

        // Audit Trail
        await _auditService.LogAsync("Update", "Lead", lead.LeadId.ToString(),
            new { OldStatus = "Qualified" },
            new { NewStatus = "Converted", ConvertedCustomerId = customer.CustomerId, ConvertedOpportunityId = opp?.OpportunityId },
            $"Lead '{lead.LeadName}' ({lead.LeadCode}) converted into Customer '{customer.CustomerName}' and Opportunity '{opp?.OpportunityName}'");

        TempData["SuccessMessage"] = $"Lead successfully converted! Customer '{customer.CustomerName}' created.";
        return RedirectToAction("Details", "Customers", new { id = customer.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && lead.AssignedTo != _currentUserService.UserId)
        {
            return Forbid();
        }

        var snapshot = new { lead.LeadId, lead.LeadName, lead.Email };

        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Lead", id.ToString(),
            snapshot, null, $"Deleted Lead '{lead.LeadName}'");

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' removed successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SelectList> GetSalesUsersListAsync(string? selectedId = null)
    {
        var users = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        return new SelectList(users, "Id", "FullName", selectedId);
    }
}
