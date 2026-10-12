using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<LoginResponseDto>.Fail("Email and password are required."));
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
        {
            await _auditService.LogAsync("Failed Login", "Auth", null, null, null,
                $"API Login failed for '{dto.Email}'", null, dto.Email);
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid credentials or inactive account."));
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, dto.Password, false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "SalesExecutive";

            await _auditService.LogAsync("Login", "Auth", user.Id, null, null,
                $"API Login successful for '{user.Email}'", user.Id, user.Email);

            return Ok(ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = primaryRole
            }, "Authentication successful"));
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync("Security", "Auth", user.Id, null, null,
                $"API Account locked out for '{user.Email}'", user.Id, user.Email);
            return StatusCode(423, ApiResponse<LoginResponseDto>.Fail("Account is temporarily locked out due to multiple failed attempts."));
        }

        await _auditService.LogAsync("Failed Login", "Auth", user.Id, null, null,
            $"API Invalid password for '{user.Email}'", user.Id, user.Email);

        return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid credentials."));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(ApiResponse<bool>.Ok(true, "Logged out successfully"));
    }
}
