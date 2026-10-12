using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using AcxiomCRM.Controllers;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Xunit;

namespace AcxiomCRM.Tests;

public class DashboardRegressionTests
{
    private (ApplicationDbContext db, UserManager<ApplicationUser> userManager) CreateContextAndUserManager()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string>(context);
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new NullLogger<UserManager<ApplicationUser>>());

        return (context, userManager);
    }

    private void SetupControllerContext(DashboardController controller, string userId, string role, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task Dashboard_AggregatesDecimalAmounts_WithoutSqliteNotSupportedException()
    {
        var (db, userManager) = CreateContextAndUserManager();
        using (db)
        {
            var admin = new ApplicationUser
            {
                Id = "admin-1",
                UserName = "admin@crm.local",
                Email = "admin@crm.local",
                FullName = "Admin User"
            };
            await userManager.CreateAsync(admin, "Admin@12345!");

            var now = DateTime.UtcNow;

            // Seed opportunities with specific decimal amounts
            db.Opportunities.AddRange(
                new Opportunity
                {
                    OpportunityName = "Deal A",
                    Amount = 10500.50m,
                    Probability = 50,
                    Stage = "Proposal",
                    Status = "Open",
                    ExpectedCloseDate = now.AddDays(15),
                    AssignedTo = admin.Id
                },
                new Opportunity
                {
                    OpportunityName = "Deal B",
                    Amount = 24499.50m,
                    Probability = 80,
                    Stage = "Negotiation",
                    Status = "Open",
                    ExpectedCloseDate = now.AddDays(25),
                    AssignedTo = admin.Id
                },
                new Opportunity
                {
                    OpportunityName = "Deal C (Won)",
                    Amount = 15000.75m,
                    Probability = 100,
                    Stage = "Won",
                    Status = "Won",
                    ExpectedCloseDate = new DateTime(now.Year, now.Month, 5),
                    AssignedTo = admin.Id
                }
            );
            await db.SaveChangesAsync();

            var currentUserService = new MockCurrentUserService
            {
                UserId = admin.Id,
                Role = "Admin",
                Email = admin.Email
            };

            var controller = new DashboardController(db, currentUserService, userManager);
            SetupControllerContext(controller, admin.Id, "Admin", admin.Email!);

            // Action: Load Dashboard - this previously crashed with NotSupportedException on SQLite
            var actionResult = await controller.Index("all");

            var viewResult = Assert.IsType<ViewResult>(actionResult);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

            // Assertions for Decimal Calculations
            // Total pipeline: Deal A (10500.50) + Deal B (24499.50) = 35000.00
            Assert.Equal(35000.00m, model.TotalPipelineValue);

            // Weighted pipeline: (10500.50 * 0.50 = 5250.25) + (24499.50 * 0.80 = 19599.60) = 24849.85
            Assert.Equal(24849.85m, model.WeightedPipelineValue);

            // Stage data dictionary
            Assert.Equal(10500.50m, model.OpportunityStageData["Proposal"]);
            Assert.Equal(24499.50m, model.OpportunityStageData["Negotiation"]);
            Assert.Equal(15000.75m, model.OpportunityStageData["Won"]);

            // Monthly sales data
            var currentMonthName = now.ToString("MMM yyyy");
            Assert.True(model.MonthlySalesData.ContainsKey(currentMonthName));
            Assert.Equal(15000.75m, model.MonthlySalesData[currentMonthName]);
        }
    }

    [Fact]
    public async Task Dashboard_SalesExecutiveScope_AggregatesOnlyAssignedDecimalAmounts()
    {
        var (db, userManager) = CreateContextAndUserManager();
        using (db)
        {
            var rep1 = new ApplicationUser { Id = "rep-1", UserName = "rep1@crm.local", Email = "rep1@crm.local", FullName = "Sales Rep 1" };
            var rep2 = new ApplicationUser { Id = "rep-2", UserName = "rep2@crm.local", Email = "rep2@crm.local", FullName = "Sales Rep 2" };
            await userManager.CreateAsync(rep1, "Sales@12345!");
            await userManager.CreateAsync(rep2, "Sales@12345!");

            var now = DateTime.UtcNow;

            db.Opportunities.AddRange(
                new Opportunity
                {
                    OpportunityName = "Rep 1 Deal",
                    Amount = 12000.00m,
                    Probability = 50,
                    Stage = "Proposal",
                    Status = "Open",
                    ExpectedCloseDate = now.AddDays(10),
                    AssignedTo = "rep-1"
                },
                new Opportunity
                {
                    OpportunityName = "Rep 2 Deal",
                    Amount = 88000.00m,
                    Probability = 70,
                    Stage = "Negotiation",
                    Status = "Open",
                    ExpectedCloseDate = now.AddDays(20),
                    AssignedTo = "rep-2"
                }
            );
            await db.SaveChangesAsync();

            // When logged in as Rep 1 (SalesExecutive)
            var currentUserService = new MockCurrentUserService
            {
                UserId = "rep-1",
                Role = "SalesExecutive",
                Email = rep1.Email
            };

            var controller = new DashboardController(db, currentUserService, userManager);
            SetupControllerContext(controller, "rep-1", "SalesExecutive", rep1.Email!);

            var actionResult = await controller.Index("all");
            var viewResult = Assert.IsType<ViewResult>(actionResult);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

            // Rep 1 should only see their 12000 pipeline, NOT Rep 2's 88000
            Assert.Equal(1, model.TotalOpportunities);
            Assert.Equal(12000.00m, model.TotalPipelineValue);
            Assert.Equal(6000.00m, model.WeightedPipelineValue);
            Assert.Equal(12000.00m, model.OpportunityStageData["Proposal"]);
            Assert.Equal(0m, model.OpportunityStageData["Negotiation"]);
        }
    }
}
