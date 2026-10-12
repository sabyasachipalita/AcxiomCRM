using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Controllers.Api;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests;

public class RoleScopingAndApiTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task SalesExecutive_IsScopedToOwnRecords_WhenQueryingCustomers()
    {
        using var db = CreateInMemoryDbContext();

        var user1 = new ApplicationUser { Id = "sales-1", UserName = "sales1@crm.local", FullName = "Sales Rep 1" };
        var user2 = new ApplicationUser { Id = "sales-2", UserName = "sales2@crm.local", FullName = "Sales Rep 2" };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        db.Customers.AddRange(
            new Customer { CustomerCode = "CUST-0001", CustomerName = "John's Account", Email = "j@test.com", Phone = "1111111", OwnerId = "sales-1", Status = "Active" },
            new Customer { CustomerCode = "CUST-0002", CustomerName = "Jane's Account", Email = "k@test.com", Phone = "2222222", OwnerId = "sales-2", Status = "Active" }
        );
        await db.SaveChangesAsync();

        var salesUser = new MockCurrentUserService { UserId = "sales-1", Role = "SalesExecutive" };
        var validation = new CrmValidationService(db);
        var audit = new AuditService(db, salesUser, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditService>.Instance);

        var controller = new CustomersApiController(db, validation, audit, salesUser);

        var response = await controller.GetCustomers(null, null);
        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<CustomerDto>>>(okResult.Value);

        Assert.Single(apiResponse.Data!);
        Assert.Equal("John's Account", apiResponse.Data![0].CustomerName);
    }

    [Fact]
    public async Task Admin_CanSeeAllCustomers()
    {
        using var db = CreateInMemoryDbContext();

        var user1 = new ApplicationUser { Id = "sales-1", UserName = "sales1@crm.local", FullName = "Sales Rep 1" };
        var user2 = new ApplicationUser { Id = "sales-2", UserName = "sales2@crm.local", FullName = "Sales Rep 2" };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        db.Customers.AddRange(
            new Customer { CustomerCode = "CUST-0001", CustomerName = "Account One", Email = "a1@test.com", Phone = "1111111", OwnerId = "sales-1", Status = "Active" },
            new Customer { CustomerCode = "CUST-0002", CustomerName = "Account Two", Email = "a2@test.com", Phone = "2222222", OwnerId = "sales-2", Status = "Active" }
        );
        await db.SaveChangesAsync();

        var adminUser = new MockCurrentUserService { UserId = "admin-1", Role = "Admin" };
        var validation = new CrmValidationService(db);
        var audit = new AuditService(db, adminUser, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditService>.Instance);

        var controller = new CustomersApiController(db, validation, audit, adminUser);

        var response = await controller.GetCustomers(null, null);
        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<CustomerDto>>>(okResult.Value);

        Assert.Equal(2, apiResponse.Data!.Count);
    }

    [Fact]
    public async Task Api_CreateCustomer_ReturnsSuccessAndAutoGeneratesCustomerCode()
    {
        using var db = CreateInMemoryDbContext();
        var adminUser = new MockCurrentUserService { UserId = "admin-1", Role = "Admin" };
        var validation = new CrmValidationService(db);
        var audit = new AuditService(db, adminUser, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditService>.Instance);

        var controller = new CustomersApiController(db, validation, audit, adminUser);

        var createDto = new CreateCustomerDto
        {
            CustomerName = "Enterprise Customer",
            Email = "corp@customer.com",
            Phone = "+91 9123456780",
            CompanyName = "Enterprise Inc",
            City = "Dallas",
            State = "TX",
            Status = "Active"
        };

        var response = await controller.CreateCustomer(createDto);
        var createdResult = Assert.IsType<CreatedAtActionResult>(response.Result);
        var apiResponse = Assert.IsType<ApiResponse<CustomerDto>>(createdResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("CUST-0001", apiResponse.Data.CustomerCode);
        Assert.Equal("Enterprise Customer", apiResponse.Data.CustomerName);
    }
}
