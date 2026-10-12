using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roleNames = { "Admin", "Manager", "SalesExecutive" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName, $"{roleName} Role"));
            }
        }

        // 2. Seed Users
        var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.local");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@acxiomcrm.local",
                Email = "admin@acxiomcrm.local",
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.local");
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = "manager@acxiomcrm.local",
                Email = "manager@acxiomcrm.local",
                FullName = "Sarah Connor (Sales Manager)",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
        }

        var salesUser1 = await userManager.FindByEmailAsync("sales@acxiomcrm.local");
        if (salesUser1 == null)
        {
            salesUser1 = new ApplicationUser
            {
                UserName = "sales@acxiomcrm.local",
                Email = "sales@acxiomcrm.local",
                FullName = "John Doe (Sales Executive)",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(salesUser1, "Sales@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser1, "SalesExecutive");
            }
        }

        var salesUser2 = await userManager.FindByEmailAsync("jane.sales@acxiomcrm.local");
        if (salesUser2 == null)
        {
            salesUser2 = new ApplicationUser
            {
                UserName = "jane.sales@acxiomcrm.local",
                Email = "jane.sales@acxiomcrm.local",
                FullName = "Jane Smith (Sales Executive)",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(salesUser2, "Sales@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser2, "SalesExecutive");
            }
        }

        // 3. Seed Customers
        if (!await context.Customers.AnyAsync())
        {
            var customers = new List<Customer>
            {
                new()
                {
                    CustomerCode = "CUST-0001",
                    CustomerName = "Acme Technologies Pvt Ltd",
                    Email = "contact@acme.com",
                    Phone = "+91 9876543210",
                    CompanyName = "Acme Technologies",
                    Address = "104 Cyber Towers, Hitec City",
                    City = "Hyderabad",
                    State = "Telangana",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy = adminUser.Email!,
                    OwnerId = salesUser1.Id,
                    Notes = "Strategic enterprise client evaluating modernization."
                },
                new()
                {
                    CustomerCode = "CUST-0002",
                    CustomerName = "Contoso Solutions",
                    Email = "accounts@contoso.com",
                    Phone = "+91 9811223344",
                    CompanyName = "Contoso Ltd",
                    Address = "Building 4, Electronic City",
                    City = "Bengaluru",
                    State = "Karnataka",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-1),
                    CreatedBy = adminUser.Email!,
                    OwnerId = salesUser2.Id,
                    Notes = "Renewed multi-year maintenance agreement."
                },
                new()
                {
                    CustomerCode = "CUST-0003",
                    CustomerName = "Fabrikam Industries",
                    Email = "procurement@fabrikam.com",
                    Phone = "+91 9922334455",
                    CompanyName = "Fabrikam Group",
                    Address = "72 Industrial Area",
                    City = "Pune",
                    State = "Maharashtra",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    CreatedBy = adminUser.Email!,
                    OwnerId = salesUser1.Id,
                    Notes = "Interested in CRM and supply chain integration."
                },
                new()
                {
                    CustomerCode = "CUST-0004",
                    CustomerName = "Northwind Traders",
                    Email = "orders@northwind.com",
                    Phone = "+91 9933445566",
                    CompanyName = "Northwind Wholesale",
                    Address = "5 Marine Lines",
                    City = "Mumbai",
                    State = "Maharashtra",
                    Status = "Inactive",
                    CreatedDate = DateTime.UtcNow.AddMonths(-3),
                    CreatedBy = adminUser.Email!,
                    OwnerId = salesUser2.Id,
                    Notes = "Dormant account; review in Q3."
                }
            };
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        // 4. Seed Leads
        if (!await context.Leads.AnyAsync())
        {
            var leads = new List<Lead>
            {
                new()
                {
                    LeadCode = "LEAD-0001",
                    LeadName = "Global Logistics Network",
                    Email = "info@globallogistics.com",
                    Phone = "+91 9845012345",
                    CompanyName = "Global Logistics",
                    Source = "Website",
                    Status = "Qualified",
                    Priority = "High",
                    ExpectedValue = 65000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-15),
                    AssignedTo = salesUser1.Id,
                    Notes = "Requesting custom quote for multi-warehouse tracking."
                },
                new()
                {
                    LeadCode = "LEAD-0002",
                    LeadName = "Apex Fintech Labs",
                    Email = "founders@apexfintech.io",
                    Phone = "+91 9765432109",
                    CompanyName = "Apex Fintech",
                    Source = "Referral",
                    Status = "Contacted",
                    Priority = "Urgent",
                    ExpectedValue = 90000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-7),
                    AssignedTo = salesUser2.Id,
                    Notes = "Introductory call completed; demo scheduled."
                },
                new()
                {
                    LeadCode = "LEAD-0003",
                    LeadName = "Zenith Healthcare Services",
                    Email = "admin@zenithhealth.org",
                    Phone = "+91 9654321098",
                    CompanyName = "Zenith Healthcare",
                    Source = "Campaign",
                    Status = "New",
                    Priority = "Medium",
                    ExpectedValue = 35000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    AssignedTo = salesUser1.Id,
                    Notes = "Inbound lead from Healthcare Summit."
                },
                new()
                {
                    LeadCode = "LEAD-0004",
                    LeadName = "Omni Retail Outlets",
                    Email = "contact@omniretail.in",
                    Phone = "+91 9543210987",
                    CompanyName = "Omni Retail",
                    Source = "Cold Call",
                    Status = "Lost",
                    Priority = "Low",
                    ExpectedValue = 12000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    AssignedTo = salesUser2.Id,
                    Notes = "Selected local competitor due to budget constraints."
                }
            };
            await context.Leads.AddRangeAsync(leads);
            await context.SaveChangesAsync();
        }

        // 5. Seed Opportunities
        if (!await context.Opportunities.AnyAsync())
        {
            var acme = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0001");
            var contoso = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0002");
            var fabrikam = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0003");

            var opportunities = new List<Opportunity>
            {
                new()
                {
                    OpportunityName = "Enterprise ERP Transformation",
                    CustomerId = acme?.CustomerId,
                    Amount = 120000m,
                    Stage = "Negotiation",
                    Probability = 80,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(20),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow.AddDays(-25),
                    AssignedTo = salesUser1.Id,
                    Notes = "Final contract terms being reviewed by legal team."
                },
                new()
                {
                    OpportunityName = "Cloud Security Migration Suite",
                    CustomerId = fabrikam?.CustomerId,
                    Amount = 85000m,
                    Stage = "Proposal",
                    Probability = 50,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(40),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow.AddDays(-14),
                    AssignedTo = salesUser1.Id,
                    Notes = "RFP submitted; technical presentation next Thursday."
                },
                new()
                {
                    OpportunityName = "Annual Analytics SaaS Renewal",
                    CustomerId = contoso?.CustomerId,
                    Amount = 45000m,
                    Stage = "Won",
                    Probability = 100,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
                    Status = "Won",
                    CreatedDate = DateTime.UtcNow.AddDays(-45),
                    AssignedTo = salesUser2.Id,
                    Notes = "Successfully signed 3-year agreement."
                },
                new()
                {
                    OpportunityName = "AI Automation Pilot",
                    CustomerId = acme?.CustomerId,
                    Amount = 25000m,
                    Stage = "Qualification",
                    Probability = 20,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(60),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    AssignedTo = salesUser1.Id,
                    Notes = "Early requirements discovery underway."
                }
            };
            await context.Opportunities.AddRangeAsync(opportunities);
            await context.SaveChangesAsync();
        }

        // 6. Seed FollowUps
        if (!await context.FollowUps.AnyAsync())
        {
            var acme = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0001");
            var lead = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-0001");
            var opp = await context.Opportunities.FirstOrDefaultAsync();

            var followUps = new List<FollowUp>
            {
                new()
                {
                    CustomerId = acme?.CustomerId,
                    OpportunityId = opp?.OpportunityId,
                    FollowUpDate = DateTime.UtcNow.AddDays(2),
                    FollowUpType = "Meeting",
                    Subject = "Finalize ERP pricing and payment milestones",
                    Remarks = "Meet with CFO and Head of IT at client HQ.",
                    Status = "Planned",
                    AssignedTo = salesUser1.Id,
                    CreatedDate = DateTime.UtcNow
                },
                new()
                {
                    LeadId = lead?.LeadId,
                    FollowUpDate = DateTime.UtcNow.AddDays(5),
                    FollowUpType = "Call",
                    Subject = "Product Demo Q&A Call",
                    Remarks = "Walkthrough architecture and security compliance.",
                    Status = "Planned",
                    AssignedTo = salesUser1.Id,
                    CreatedDate = DateTime.UtcNow
                },
                new()
                {
                    CustomerId = acme?.CustomerId,
                    FollowUpDate = DateTime.UtcNow.AddDays(-2),
                    FollowUpType = "Email",
                    Subject = "Sent standard NDA and pricing proposal",
                    Remarks = "Delivered via email; acknowledged by procurement.",
                    Status = "Completed",
                    AssignedTo = salesUser1.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-3)
                }
            };
            await context.FollowUps.AddRangeAsync(followUps);
            await context.SaveChangesAsync();
        }

        // 7. Seed Activities
        if (!await context.Activities.AnyAsync())
        {
            var acme = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0001");
            var lead = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-0002");

            var activities = new List<Activity>
            {
                new()
                {
                    ActivityType = "Call",
                    Subject = "Introductory Discovery Call",
                    Description = "Discussed business goals and current pain points.",
                    ActivityDate = DateTime.UtcNow.AddDays(-4),
                    CustomerId = acme?.CustomerId,
                    AssignedTo = salesUser1.Id,
                    Status = "Completed"
                },
                new()
                {
                    ActivityType = "Meeting",
                    Subject = "Technical Architecture Alignment",
                    Description = "Demonstrated integration capabilities and security framework.",
                    ActivityDate = DateTime.UtcNow.AddDays(-1),
                    LeadId = lead?.LeadId,
                    AssignedTo = salesUser2.Id,
                    Status = "Completed"
                }
            };
            await context.Activities.AddRangeAsync(activities);
            await context.SaveChangesAsync();
        }

        // 8. Seed Audit Log
        if (!await context.AuditLogs.AnyAsync())
        {
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    UserId = adminUser.Id,
                    UserName = adminUser.Email,
                    Action = "Security",
                    EntityName = "System",
                    RecordId = "INIT",
                    Details = "AcxiomCRM Database Initialized with baseline security policies and master entities.",
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    IpAddress = "127.0.0.1"
                },
                new AuditLog
                {
                    UserId = adminUser.Id,
                    UserName = adminUser.Email,
                    Action = "Role Change",
                    EntityName = "User",
                    RecordId = salesUser1.Id,
                    Details = "Provisioned SalesExecutive role for John Doe.",
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    IpAddress = "127.0.0.1"
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
