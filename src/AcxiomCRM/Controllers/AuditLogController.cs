using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AuditLogController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogController(ApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(
        string? userFilter,
        string? entityFilter,
        string? actionFilter,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1)
    {
        const int pageSize = 20;
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        // Manager limited view: cannot see security role changes or password reset logs
        if (_currentUserService.IsManager && !_currentUserService.IsAdmin)
        {
            query = query.Where(a => a.EntityName != "User" && a.Action != "Security");
        }

        if (!string.IsNullOrWhiteSpace(userFilter))
        {
            var u = userFilter.Trim().ToLower();
            query = query.Where(a => a.UserName != null && a.UserName.ToLower().Contains(u));
        }

        if (!string.IsNullOrWhiteSpace(entityFilter))
        {
            query = query.Where(a => a.EntityName == entityFilter);
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action == actionFilter);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var logs = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.UserFilter = userFilter;
        ViewBag.EntityFilter = entityFilter;
        ViewBag.ActionFilter = actionFilter;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        return View(logs);
    }
}
