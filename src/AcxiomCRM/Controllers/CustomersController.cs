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
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICrmValidationService _validationService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(
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

    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? ownerFilter, int page = 1)
    {
        const int pageSize = 10;
        var query = _db.Customers.Include(c => c.Owner).AsQueryable();

        // Role-based scoping
        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(c => c.OwnerId == _currentUserService.UserId);
        }
        else if (!string.IsNullOrEmpty(ownerFilter))
        {
            query = query.Where(c => c.OwnerId == ownerFilter);
        }

        // Filtering
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Phone.Contains(term) ||
                c.CustomerCode.ToLower().Contains(term) ||
                c.CompanyName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(c => c.Status == statusFilter);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var customers = await query
            .OrderByDescending(c => c.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var salesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        var ownersList = new SelectList(salesUsers, "Id", "FullName", ownerFilter);

        var model = new CustomerListViewModel
        {
            Customers = customers,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            OwnerFilter = ownerFilter,
            OwnersList = ownersList,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return NotFound();

        // Role scoping check
        if (_currentUserService.IsSalesExecutive && customer.OwnerId != _currentUserService.UserId)
        {
            return Forbid();
        }

        var opportunities = await _db.Opportunities
            .Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();

        var followUps = await _db.FollowUps
            .Where(f => f.CustomerId == id)
            .OrderByDescending(f => f.FollowUpDate)
            .ToListAsync();

        var activities = await _db.Activities
            .Where(a => a.CustomerId == id)
            .OrderByDescending(a => a.ActivityDate)
            .ToListAsync();

        var model = new CustomerDetailsViewModel
        {
            Customer = customer,
            Opportunities = opportunities,
            FollowUps = followUps,
            Activities = activities
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CustomerFormViewModel
        {
            Status = "Active",
            OwnerId = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : null,
            OwnersList = await GetSalesUsersListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        var customer = new Customer
        {
            CustomerName = model.CustomerName,
            Email = model.Email,
            Phone = model.Phone,
            CompanyName = model.CompanyName ?? string.Empty,
            Address = model.Address ?? string.Empty,
            City = model.City ?? string.Empty,
            State = model.State ?? string.Empty,
            Status = model.Status,
            Notes = model.Notes,
            CreatedBy = _currentUserService.Email ?? "System",
            OwnerId = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : model.OwnerId,
            CreatedDate = DateTime.UtcNow
        };

        // Business Validation
        var validation = await _validationService.ValidateCustomerAsync(customer);
        foreach (var err in validation.Errors)
        {
            ModelState.AddModelError(err.Key, err.Value);
        }

        if (!ModelState.IsValid)
        {
            model.OwnersList = await GetSalesUsersListAsync(model.OwnerId);
            return View(model);
        }

        // Generate CustomerCode
        var lastId = await _db.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
        customer.CustomerCode = $"CUST-{(lastId + 1):D4}";

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Customer", customer.CustomerId.ToString(),
            null, new { customer.CustomerCode, customer.CustomerName, customer.Email, customer.Phone },
            $"Created Customer '{customer.CustomerName}' ({customer.CustomerCode})");

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && customer.OwnerId != _currentUserService.UserId)
        {
            return Forbid();
        }

        var model = new CustomerFormViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            OwnerId = customer.OwnerId,
            Notes = customer.Notes,
            OwnersList = await GetSalesUsersListAsync(customer.OwnerId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
    {
        if (id != model.CustomerId) return BadRequest();

        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && customer.OwnerId != _currentUserService.UserId)
        {
            return Forbid();
        }

        var oldSnapshot = new
        {
            customer.CustomerName,
            customer.Email,
            customer.Phone,
            customer.Status,
            customer.OwnerId
        };

        customer.CustomerName = model.CustomerName;
        customer.Email = model.Email;
        customer.Phone = model.Phone;
        customer.CompanyName = model.CompanyName ?? string.Empty;
        customer.Address = model.Address ?? string.Empty;
        customer.City = model.City ?? string.Empty;
        customer.State = model.State ?? string.Empty;
        customer.Status = model.Status;
        customer.Notes = model.Notes;

        if (!_currentUserService.IsSalesExecutive)
        {
            customer.OwnerId = model.OwnerId;
        }

        // Server-side & Business Validation
        var validation = await _validationService.ValidateCustomerAsync(customer, existingId: id);
        foreach (var err in validation.Errors)
        {
            ModelState.AddModelError(err.Key, err.Value);
        }

        if (!ModelState.IsValid)
        {
            model.OwnersList = await GetSalesUsersListAsync(model.OwnerId);
            return View(model);
        }

        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();

        var newSnapshot = new
        {
            customer.CustomerName,
            customer.Email,
            customer.Phone,
            customer.Status,
            customer.OwnerId
        };

        await _auditService.LogAsync("Update", "Customer", customer.CustomerId.ToString(),
            oldSnapshot, newSnapshot, $"Updated Customer '{customer.CustomerName}'");

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        if (_currentUserService.IsSalesExecutive && customer.OwnerId != _currentUserService.UserId)
        {
            return Forbid();
        }

        var snapshot = new { customer.CustomerId, customer.CustomerName, customer.Email };

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Customer", id.ToString(),
            snapshot, null, $"Deleted Customer '{customer.CustomerName}'");

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' removed successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SelectList> GetSalesUsersListAsync(string? selectedId = null)
    {
        var users = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        return new SelectList(users, "Id", "FullName", selectedId);
    }
}
