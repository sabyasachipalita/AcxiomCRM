namespace AcxiomCRM.ViewModels;

public class PipelineReportItem
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class OwnerPipelineItem
{
    public string OwnerName { get; set; } = string.Empty;
    public int OpenOpportunities { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class PipelineReportViewModel
{
    public List<PipelineReportItem> StageSummary { get; set; } = new();
    public List<OwnerPipelineItem> OwnerSummary { get; set; } = new();
    public decimal GrandTotalAmount { get; set; }
    public decimal GrandWeightedAmount { get; set; }
}

public class ConversionReportViewModel
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public decimal LeadConversionRate => TotalLeads > 0 ? Math.Round((decimal)ConvertedLeads / TotalLeads * 100m, 1) : 0;

    public int TotalClosedOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal WinRate => TotalClosedOpportunities > 0 ? Math.Round((decimal)WonOpportunities / TotalClosedOpportunities * 100m, 1) : 0;
}

public class UserActivityReportItem
{
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CustomersManaged { get; set; }
    public int LeadsAssigned { get; set; }
    public int OpportunitiesManaged { get; set; }
    public int FollowUpsLogged { get; set; }
    public int ActivitiesLogged { get; set; }
}
