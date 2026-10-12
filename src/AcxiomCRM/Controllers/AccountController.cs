using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IAuditService auditService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            await _auditService.LogAsync("Failed Login", "Auth", null, null, null,
                $"Failed login attempt for nonexistent user '{model.Email}'", null, model.Email);
            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        if (!user.IsActive)
        {
            await _auditService.LogAsync("Failed Login", "Auth", user.Id, null, null,
                $"Login rejected for deactivated user '{user.Email}'", user.Id, user.Email);
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
            return View(model);
        }

        // Lockout on failure is enabled
        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditService.LogAsync("Login", "Auth", user.Id, null, null,
                $"User '{user.Email}' successfully logged in", user.Id, user.Email);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync("Security", "Auth", user.Id, null, null,
                $"Account locked out for user '{user.Email}' due to repeated failed login attempts", user.Id, user.Email);
            ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts. Try again in 15 minutes.");
            return View(model);
        }

        await _auditService.LogAsync("Failed Login", "Auth", user.Id, null, null,
            $"Invalid password attempt for user '{user.Email}'", user.Id, user.Email);

        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email address is already registered.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            var roleToAssign = string.IsNullOrEmpty(model.Role) ? "SalesExecutive" : model.Role;
            if (!await _roleManager.RoleExistsAsync(roleToAssign))
            {
                roleToAssign = "SalesExecutive";
            }

            await _userManager.AddToRoleAsync(user, roleToAssign);

            await _auditService.LogAsync("Create", "User", user.Id, null, new { user.Email, Role = roleToAssign },
                $"User '{user.Email}' registered with role '{roleToAssign}'", user.Id, user.Email);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            await _auditService.LogAsync("Logout", "Auth", user.Id, null, null,
                $"User '{user.Email}' logged out", user.Id, user.Email);
        }

        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
