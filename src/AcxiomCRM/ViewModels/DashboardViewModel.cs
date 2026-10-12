using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    public string UserRole { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FilterPeriod { get; set; } = "all"; // all, today, week, month

    // KPI Cards
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }

    // Chart Data - Lead Status
    public Dictionary<string, int> LeadStatusData { get; set; } = new();

    // Chart Data - Opportunity Pipeline
    public Dictionary<string, decimal> OpportunityStageData { get; set; } = new();

    // Chart Data - Monthly Sales
    public Dictionary<string, decimal> MonthlySalesData { get; set; } = new();

    // Recent items & reminders
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<FollowUp> OverdueFollowUps { get; set; } = new();
    public List<Activity> RecentActivities { get; set; } = new();
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
}
