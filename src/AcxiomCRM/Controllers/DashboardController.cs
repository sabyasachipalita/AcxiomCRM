
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

        // 1. Date range filter
        DateTime? minDate = null;
        var now = DateTime.UtcNow;

        switch (period?.ToLowerInvariant())
        {
            case "today":
                minDate = now.Date;
                break;

            case "week":
                minDate = now.Date.AddDays(-(int)now.DayOfWeek);
                break;

            case "month":
                minDate = new DateTime(now.Year, now.Month, 1);
                break;
        }

        // 2. Build role-scoped queries
        IQueryable<Customer> custQuery =
            _db.Customers.AsNoTracking();

        IQueryable<Lead> leadQuery =
            _db.Leads.AsNoTracking();

        IQueryable<Opportunity> oppQuery =
            _db.Opportunities.AsNoTracking();

        IQueryable<FollowUp> followUpQuery =
            _db.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .AsNoTracking();

        IQueryable<Activity> activityQuery =
            _db.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .AsNoTracking();

        // Restrict Sales Executives to their assigned records.
        if (isSalesExec)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Fail closed if the current user's ID is unavailable.
                custQuery = custQuery.Where(c => false);
                leadQuery = leadQuery.Where(l => false);
                oppQuery = oppQuery.Where(o => false);
                followUpQuery = followUpQuery.Where(f => false);
                activityQuery = activityQuery.Where(a => false);
            }
            else
            {
                custQuery = custQuery.Where(c => c.OwnerId == userId);
                leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
                oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
                followUpQuery = followUpQuery.Where(f => f.AssignedTo == userId);
                activityQuery = activityQuery.Where(a => a.AssignedTo == userId);
            }
        }

        // Apply date filtering to the same entities as the original implementation.
        if (minDate.HasValue)
        {
            var startDate = minDate.Value;

            custQuery = custQuery.Where(c => c.CreatedDate >= startDate);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate);
        }

        // 3. KPI counts
        var totalCustomers = await custQuery.CountAsync();

        var totalLeads = await leadQuery.CountAsync();

        var openLeads = await leadQuery.CountAsync(
            l => l.Status != "Converted" && l.Status != "Lost");

        var totalOpportunities = await oppQuery.CountAsync();

        var openOpportunities = await oppQuery.CountAsync(
            o => o.Status == "Open");

        var wonOpportunities = await oppQuery.CountAsync(
            o => o.Status == "Won" || o.Stage == "Won");

        var lostOpportunities = await oppQuery.CountAsync(
            o => o.Status == "Lost" || o.Stage == "Lost");

        // SQLite cannot translate Sum(decimal) in this query.
        // Materialize only filtered, authorized records, then aggregate in C#.
        var openOppsList = await oppQuery
            .Where(o => o.Status == "Open")
            .Select(o => new
            {
                o.Amount,
                o.Probability
            })
            .ToListAsync();

        var totalPipelineValue = openOppsList.Sum(o => o.Amount);

        var weightedPipelineValue = openOppsList.Sum(o =>
            Math.Round(o.Amount * o.Probability / 100m, 2));

        // 4. Lead status chart
        var leadStatusCounts = await leadQuery
            .GroupBy(l => l.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(g => g.Status, g => g.Count);

        var configuredStatuses = new[]
        {
            "New", "Contacted", "Qualified",
            "Unqualified", "Converted", "Lost"
        };

        var leadStatusData = new Dictionary<string, int>();

        foreach (var status in configuredStatuses)
        {
            leadStatusData[status] =
                leadStatusCounts.TryGetValue(status, out var count)
                    ? count
                    : 0;
        }

        // 5. Opportunity pipeline chart.
        // Aggregate decimal amounts in memory for SQLite compatibility.
        var opportunityStageItems = await oppQuery
            .Select(o => new
            {
                o.Stage,
                o.Amount
            })
            .ToListAsync();

        var opportunityStageTotals = opportunityStageItems
            .GroupBy(o => o.Stage)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(o => o.Amount));

        var configuredStages = new[]
        {
            "Qualification", "Proposal",
            "Negotiation", "Won", "Lost"
        };

        var opportunityStageData = new Dictionary<string, decimal>();

        foreach (var stage in configuredStages)
        {
            opportunityStageData[stage] =
                opportunityStageTotals.TryGetValue(stage, out var amount)
                    ? amount
                    : 0m;
        }

        // 6. Monthly sales chart: current month and previous five months.
        var sixMonthsAgo = new DateTime(now.Year, now.Month, 1)
            .AddMonths(-5);

        var wonOpportunitiesList = await oppQuery
            .Where(o =>
                (o.Status == "Won" || o.Stage == "Won") &&
                o.ExpectedCloseDate >= sixMonthsAgo)
            .Select(o => new
            {
                o.ExpectedCloseDate,
                o.Amount
            })
            .ToListAsync();

        var monthlySalesData = new Dictionary<string, decimal>();

        for (var i = 5; i >= 0; i--)
        {
            var targetMonth = new DateTime(now.Year, now.Month, 1)
                .AddMonths(-i);

            var monthName = targetMonth.ToString("MMM yyyy");
            var monthEnd = targetMonth.AddMonths(1);

            var monthlyTotal = wonOpportunitiesList
                .Where(o =>
                    o.ExpectedCloseDate >= targetMonth &&
                    o.ExpectedCloseDate < monthEnd)
                .Sum(o => o.Amount);

            monthlySalesData[monthName] = monthlyTotal;
        }

        // 7. Upcoming and overdue follow-ups
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var upcomingFollowUps = await followUpQuery
            .Where(f =>
                f.Status == "Planned" &&
                f.FollowUpDate >= today &&
                f.FollowUpDate < tomorrow.AddYears(100))
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        var overdueFollowUps = await followUpQuery
            .Where(f =>
                f.Status == "Planned" &&
                f.FollowUpDate < today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        // 8. Recent activities
        var recentActivities = await activityQuery
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .ToListAsync();

        // 9. Recent audit records for permitted roles
        var recentAuditLogs = new List<AuditLog>();

        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            recentAuditLogs = await _db.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedDate)
                .Take(6)
                .ToListAsync();
        }

        // 10. Dashboard view model
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
            OpportunityStageData = opportunityStageData,
            MonthlySalesData = monthlySalesData,

            UpcomingFollowUps = upcomingFollowUps,
            OverdueFollowUps = overdueFollowUps,
            RecentActivities = recentActivities,
            RecentAuditLogs = recentAuditLogs
        };

        return View(model);
    }
}
