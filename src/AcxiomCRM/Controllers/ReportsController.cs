using System.Text;
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
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        ApplicationDbContext db,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Pipeline()
    {
        var oppQuery = _db.Opportunities
            .Include(o => o.Owner)
            .AsNoTracking()
            .AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            oppQuery = oppQuery.Where(o => o.AssignedTo == _currentUserService.UserId);
        }

        var opportunities = await oppQuery.ToListAsync();

        var stageGroups = opportunities
            .GroupBy(o => o.Stage)
            .Select(g => new PipelineReportItem
            {
                Stage = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedAmount)
            })
            .OrderBy(s => s.Stage)
            .ToList();

        var ownerGroups = opportunities
            .GroupBy(o => o.Owner?.FullName ?? "Unassigned")
            .Select(g => new OwnerPipelineItem
            {
                OwnerName = g.Key,
                OpenOpportunities = g.Count(x => x.Status == "Open"),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedAmount)
            })
            .OrderByDescending(o => o.TotalAmount)
            .ToList();

        var model = new PipelineReportViewModel
        {
            StageSummary = stageGroups,
            OwnerSummary = ownerGroups,
            GrandTotalAmount = opportunities.Sum(o => o.Amount),
            GrandWeightedAmount = opportunities.Sum(o => o.WeightedAmount)
        };

        return View(model);
    }

    public async Task<IActionResult> Conversion()
    {
        var leadQuery = _db.Leads.AsNoTracking().AsQueryable();
        var oppQuery = _db.Opportunities.AsNoTracking().AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            leadQuery = leadQuery.Where(l => l.AssignedTo == _currentUserService.UserId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == _currentUserService.UserId);
        }

        var totalLeads = await leadQuery.CountAsync();
        var convertedLeads = await leadQuery.CountAsync(l => l.Status == "Converted");

        var wonOpps = await oppQuery.CountAsync(o => o.Status == "Won" || o.Stage == "Won");
        var lostOpps = await oppQuery.CountAsync(o => o.Status == "Lost" || o.Stage == "Lost");
        var totalClosed = wonOpps + lostOpps;

        var model = new ConversionReportViewModel
        {
            TotalLeads = totalLeads,
            ConvertedLeads = convertedLeads,
            TotalClosedOpportunities = totalClosed,
            WonOpportunities = wonOpps,
            LostOpportunities = lostOpps
        };

        return View(model);
    }

    public async Task<IActionResult> UserActivity()
    {
        if (!_currentUserService.IsAdmin && !_currentUserService.IsManager)
        {
            return Forbid();
        }

        var users = await _userManager.Users.ToListAsync();
        var list = new List<UserActivityReportItem>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var role = roles.FirstOrDefault() ?? "None";

            var custCount = await _db.Customers.CountAsync(c => c.OwnerId == u.Id);
            var leadCount = await _db.Leads.CountAsync(l => l.AssignedTo == u.Id);
            var oppCount = await _db.Opportunities.CountAsync(o => o.AssignedTo == u.Id);
            var followUpCount = await _db.FollowUps.CountAsync(f => f.AssignedTo == u.Id);
            var activityCount = await _db.Activities.CountAsync(a => a.AssignedTo == u.Id);

            list.Add(new UserActivityReportItem
            {
                UserName = u.FullName,
                Role = role,
                CustomersManaged = custCount,
                LeadsAssigned = leadCount,
                OpportunitiesManaged = oppCount,
                FollowUpsLogged = followUpCount,
                ActivitiesLogged = activityCount
            });
        }

        return View(list);
    }

    // CSV Export for Customers
    public async Task<IActionResult> ExportCustomersCsv()
    {
        var query = _db.Customers.Include(c => c.Owner).AsNoTracking().AsQueryable();
        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(c => c.OwnerId == _currentUserService.UserId);
        }

        var customers = await query.OrderBy(c => c.CustomerCode).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("CustomerCode,CustomerName,Email,Phone,CompanyName,City,State,Status,Owner,CreatedDate");

        foreach (var c in customers)
        {
            sb.AppendLine($"\"{c.CustomerCode}\",\"{c.CustomerName}\",\"{c.Email}\",\"{c.Phone}\",\"{c.CompanyName}\",\"{c.City}\",\"{c.State}\",\"{c.Status}\",\"{c.Owner?.FullName}\",\"{c.CreatedDate:yyyy-MM-dd}\"");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"Customers_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // CSV Export for Leads
    public async Task<IActionResult> ExportLeadsCsv()
    {
        var query = _db.Leads.Include(l => l.Owner).AsNoTracking().AsQueryable();
        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(l => l.AssignedTo == _currentUserService.UserId);
        }

        var leads = await query.OrderBy(l => l.LeadCode).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("LeadCode,LeadName,Email,Phone,Company,Source,Status,Priority,ExpectedValue,AssignedTo,CreatedDate");

        foreach (var l in leads)
        {
            sb.AppendLine($"\"{l.LeadCode}\",\"{l.LeadName}\",\"{l.Email}\",\"{l.Phone}\",\"{l.CompanyName}\",\"{l.Source}\",\"{l.Status}\",\"{l.Priority}\",{l.ExpectedValue},\"{l.Owner?.FullName}\",\"{l.CreatedDate:yyyy-MM-dd}\"");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"Leads_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // CSV Export for Opportunities
    public async Task<IActionResult> ExportOpportunitiesCsv()
    {
        var query = _db.Opportunities.Include(o => o.Customer).Include(o => o.Owner).AsNoTracking().AsQueryable();
        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(o => o.AssignedTo == _currentUserService.UserId);
        }

        var opps = await query.OrderByDescending(o => o.CreatedDate).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("OpportunityName,Customer,Amount,Stage,Probability,WeightedAmount,ExpectedCloseDate,Status,AssignedTo");

        foreach (var o in opps)
        {
            sb.AppendLine($"\"{o.OpportunityName}\",\"{o.Customer?.CustomerName}\",{o.Amount},\"{o.Stage}\",{o.Probability},{o.WeightedAmount},\"{o.ExpectedCloseDate:yyyy-MM-dd}\",\"{o.Status}\",\"{o.Owner?.FullName}\"");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"Opportunities_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
