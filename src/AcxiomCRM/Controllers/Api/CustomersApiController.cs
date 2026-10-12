using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICrmValidationService _validationService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CustomersApiController(
        ApplicationDbContext db,
        ICrmValidationService validationService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _validationService = validationService;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomerDto>>>> GetCustomers([FromQuery] string? search, [FromQuery] string? status)
    {
        var query = _db.Customers.Include(c => c.Owner).AsNoTracking().AsQueryable();

        if (_currentUserService.IsSalesExecutive && !string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(c => c.OwnerId == _currentUserService.UserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(s) || c.Email.ToLower().Contains(s) || c.Phone.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var customers = await query.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            OwnerName = c.Owner != null ? c.Owner.FullName : null,
            CreatedDate = c.CreatedDate
        }).ToListAsync();

        return Ok(ApiResponse<List<CustomerDto>>.Ok(customers, $"Retrieved {customers.Count} customers"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetCustomer(int id)
    {
        var c = await _db.Customers.Include(x => x.Owner).FirstOrDefaultAsync(x => x.CustomerId == id);
        if (c == null)
        {
            return NotFound(ApiResponse<CustomerDto>.Fail($"Customer with ID {id} was not found."));
        }

        if (_currentUserService.IsSalesExecutive && c.OwnerId != _currentUserService.UserId)
        {
            return Forbid();
        }

        var dto = new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            OwnerName = c.Owner != null ? c.Owner.FullName : null,
            CreatedDate = c.CreatedDate
        };

        return Ok(ApiResponse<CustomerDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<CustomerDto>.Fail("Invalid customer data.", errors));
        }

        var customer = new Customer
        {
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName ?? string.Empty,
            Address = dto.Address ?? string.Empty,
            City = dto.City ?? string.Empty,
            State = dto.State ?? string.Empty,
            Status = dto.Status,
            OwnerId = _currentUserService.IsSalesExecutive ? _currentUserService.UserId : dto.OwnerId,
            Notes = dto.Notes,
            CreatedBy = _currentUserService.Email ?? "API",
            CreatedDate = DateTime.UtcNow
        };

        var validation = await _validationService.ValidateCustomerAsync(customer);
        if (!validation.IsValid)
        {
            return Conflict(ApiResponse<CustomerDto>.Fail("Business validation failed.", validation.Errors.Values.ToList()));
        }

        var lastId = await _db.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
        customer.CustomerCode = $"CUST-{(lastId + 1):D4}";

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Customer", customer.CustomerId.ToString(),
            null, new { customer.CustomerCode, customer.CustomerName, customer.Email },
            $"API Created Customer '{customer.CustomerName}'");

        var responseDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate
        };

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, ApiResponse<CustomerDto>.Ok(responseDto, "Customer created"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound(ApiResponse<CustomerDto>.Fail($"Customer with ID {id} was not found."));
        }

        if (_currentUserService.IsSalesExecutive && customer.OwnerId != _currentUserService.UserId)
        {
            return Forbid();
        }

        var oldSnapshot = new { customer.CustomerName, customer.Email, customer.Phone, customer.Status };

        customer.CustomerName = dto.CustomerName;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        customer.CompanyName = dto.CompanyName ?? string.Empty;
        customer.Address = dto.Address ?? string.Empty;
        customer.City = dto.City ?? string.Empty;
        customer.State = dto.State ?? string.Empty;
        customer.Status = dto.Status;
        customer.Notes = dto.Notes;
        if (!_currentUserService.IsSalesExecutive) customer.OwnerId = dto.OwnerId;

        var validation = await _validationService.ValidateCustomerAsync(customer, id);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<CustomerDto>.Fail("Business validation failed.", validation.Errors.Values.ToList()));
        }

        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Update", "Customer", customer.CustomerId.ToString(),
            oldSnapshot, new { customer.CustomerName, customer.Email, customer.Status },
            $"API Updated Customer '{customer.CustomerName}'");

        var resultDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate
        };

        return Ok(ApiResponse<CustomerDto>.Ok(resultDto, "Customer updated"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCustomer(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound(ApiResponse<bool>.Fail($"Customer with ID {id} was not found."));
        }

        if (_currentUserService.IsSalesExecutive && customer.OwnerId != _currentUserService.UserId)
        {
            return Forbid();
        }

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Customer", id.ToString(),
            new { customer.CustomerId, customer.CustomerName }, null,
            $"API Deleted Customer '{customer.CustomerName}'");

        return Ok(ApiResponse<bool>.Ok(true, "Customer deleted"));
    }
}
