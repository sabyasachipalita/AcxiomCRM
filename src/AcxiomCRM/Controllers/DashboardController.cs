using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        ApplicationDbContext db,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string period = "all")
    {
        var userId = _currentUserService.UserId;
        var isSalesExec = _currentUserService.IsSalesExecutive;
        var role = _currentUserService.Role ?? "User";

        var user = await _userManager.GetUserAsync(User);
        var fullName = user?.FullName ?? User.Identity?.Name ?? "User";

        // Date range filter
        DateTime? minDate = null;
        var now = DateTime.UtcNow;
        if (period == "today")
        {
            minDate = now.Date;
        }
        else if (period == "week")
        {
            minDate = now.Date.AddDays(-(int)now.DayOfWeek);
        }
        else if (period == "month")
        {
            minDate = new DateTime(now.Year, now.Month, 1);
        }

        // 1. Scoped Queryables
        IQueryable<Customer> custQuery = _db.Customers.AsNoTracking();
        IQueryable<Lead> leadQuery = _db.Leads.AsNoTracking();
        IQueryable<Opportunity> oppQuery = _db.Opportunities.AsNoTracking();
        IQueryable<FollowUp> followUpQuery = _db.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking();
        IQueryable<Activity> activityQuery = _db.Activities.Include(a => a.Customer).Include(a => a.Lead).AsNoTracking();

        if (isSalesExec && !string.IsNullOrEmpty(userId))
        {
            custQuery = custQuery.Where(c => c.OwnerId == userId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
            followUpQuery = followUpQuery.Where(f => f.AssignedTo == userId);
            activityQuery = activityQuery.Where(a => a.AssignedTo == userId);
        }

        if (minDate.HasValue)
        {
            custQuery = custQuery.Where(c => c.CreatedDate >= minDate.Value);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= minDate.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= minDate.Value);
        }

        // 2. Compute KPI Metrics
        var totalCustomers = await custQuery.CountAsync();
        var totalLeads = await leadQuery.CountAsync();
        var openLeads = await leadQuery.CountAsync(l => l.Status != "Converted" && l.Status != "Lost");

        var totalOpportunities = await oppQuery.CountAsync();
        var openOpportunities = await oppQuery.CountAsync(o => o.Status == "Open");
        var wonOpportunities = await oppQuery.CountAsync(o => o.Status == "Won" || o.Stage == "Won");
        var lostOpportunities = await oppQuery.CountAsync(o => o.Status == "Lost" || o.Stage == "Lost");

        var openOppsList = await oppQuery.Where(o => o.Status == "Open").Select(o => new { o.Amount, o.Probability }).ToListAsync();
        var totalPipelineValue = openOppsList.Sum(o => o.Amount);
        var weightedPipelineValue = openOppsList.Sum(o => Math.Round(o.Amount * o.Probability / 100m, 2));

        // 3. Lead Status Chart Data
        var leadStatusCounts = await leadQuery
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);

        var configuredStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
        var leadStatusData = new Dictionary<string, int>();
        foreach (var st in configuredStatuses)
        {
            leadStatusData[st] = leadStatusCounts.TryGetValue(st, out var c) ? c : 0;
        }

        // 4. Opportunity Pipeline Stages Chart Data
        var oppStageTotals = await oppQuery
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(g => g.Stage, g => g.Total);

        var configuredStages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        var oppStageData = new Dictionary<string, decimal>();
        foreach (var stg in configuredStages)
        {
            oppStageData[stg] = oppStageTotals.TryGetValue(stg, out var val) ? val : 0m;
        }

        // 5. Monthly Sales Chart Data (Last 6 Months)
        var monthlySalesData = new Dictionary<string, decimal>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var monthName = targetMonth.ToString("MMM yyyy");
            var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var monthlyTotal = await oppQuery
                .Where(o => (o.Status == "Won" || o.Stage == "Won") && o.ExpectedCloseDate >= monthStart && o.ExpectedCloseDate < monthEnd)
                .SumAsync(o => o.Amount);

            monthlySalesData[monthName] = monthlyTotal;
        }

        // 6. Upcoming & Overdue FollowUps
        var today = DateTime.UtcNow.Date;
        var upcomingFollowUps = await followUpQuery
            .Where(f => f.Status == "Planned" && f.FollowUpDate.Date >= today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        var overdueFollowUps = await followUpQuery
            .Where(f => f.Status == "Planned" && f.FollowUpDate.Date < today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        var recentActivities = await activityQuery
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .ToListAsync();

        var recentAuditLogs = new List<AuditLog>();
        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            recentAuditLogs = await _db.AuditLogs
                .OrderByDescending(a => a.CreatedDate)
                .Take(6)
                .ToListAsync();
        }

        var model = new DashboardViewModel
        {
            UserRole = role,
            UserName = fullName,
            FilterPeriod = period,
            TotalCustomers = totalCustomers,
            TotalLeads = totalLeads,
            OpenLeads = openLeads,
            TotalOpportunities = totalOpportunities,
            OpenOpportunities = openOpportunities,
            WonOpportunities = wonOpportunities,
            LostOpportunities = lostOpportunities,
            TotalPipelineValue = totalPipelineValue,
            WeightedPipelineValue = weightedPipelineValue,
            LeadStatusData = leadStatusData,
            OpportunityStageData = oppStageData,
            MonthlySalesData = monthlySalesData,
            UpcomingFollowUps = upcomingFollowUps,
            OverdueFollowUps = overdueFollowUps,
            RecentActivities = recentActivities,
            RecentAuditLogs = recentAuditLogs
        };

        return View(model);
    }
}
