using System.Security.Claims;

namespace AcxiomCRM.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsManager { get; }
    bool IsSalesExecutive { get; }
    string? IpAddress { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UserName => User?.Identity?.Name;
    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.Identity?.Name;
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public bool IsAdmin => User?.IsInRole("Admin") ?? false;
    public bool IsManager => User?.IsInRole("Manager") ?? false;
    public bool IsSalesExecutive => User?.IsInRole("SalesExecutive") ?? false;

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var header))
            {
                return header.ToString().Split(',')[0].Trim();
            }
            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
