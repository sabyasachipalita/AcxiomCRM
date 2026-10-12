using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAuditService _auditService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? roleFilter)
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UserItemViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var primaryRole = roles.FirstOrDefault() ?? "None";

            var isLocked = await _userManager.IsLockedOutAsync(u);

            userViewModels.Add(new UserItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? u.UserName ?? string.Empty,
                Role = primaryRole,
                IsActive = u.IsActive,
                IsLockedOut = isLocked,
                FailedLoginCount = u.AccessFailedCount,
                LockoutEnd = u.LockoutEnd?.UtcDateTime,
                CreatedDate = u.CreatedDate
            });
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            userViewModels = userViewModels.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            userViewModels = userViewModels.Where(u => u.Role == roleFilter).ToList();
        }

        var model = new UserListViewModel
        {
            Users = userViewModels,
            SearchTerm = searchTerm,
            RoleFilter = roleFilter
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = await _userManager.IsLockedOutAsync(user);

        var model = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = roles.FirstOrDefault() ?? "SalesExecutive",
            IsActive = user.IsActive,
            IsLockedOut = isLocked,
            RolesList = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync())
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var oldRoles = await _userManager.GetRolesAsync(user);
        var oldRole = oldRoles.FirstOrDefault() ?? "None";
        var oldStatus = user.IsActive;

        user.FullName = model.FullName;
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var err in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            model.RolesList = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync());
            return View(model);
        }

        // Role update
        if (oldRole != model.Role)
        {
            if (oldRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, oldRoles);
            }
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync("Role Change", "User", user.Id,
                new { OldRole = oldRole }, new { NewRole = model.Role },
                $"Changed role for user '{user.Email}' from '{oldRole}' to '{model.Role}'");
        }

        if (oldStatus != model.IsActive)
        {
            await _auditService.LogAsync("Security", "User", user.Id,
                new { IsActive = oldStatus }, new { IsActive = model.IsActive },
                $"{(model.IsActive ? "Activated" : "Deactivated")} user account '{user.Email}'");
        }

        TempData["SuccessMessage"] = $"User '{user.Email}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync("Security", "User", user.Id,
            null, null, $"Admin unlocked account for '{user.Email}'");

        TempData["SuccessMessage"] = $"Account '{user.Email}' unlocked successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        return View(new ResetPasswordViewModel
        {
            UserId = user.Id,
            Email = user.Email!
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (result.Succeeded)
        {
            await _auditService.LogAsync("Security", "User", user.Id,
                null, null, $"Admin reset password for user '{user.Email}'");

            TempData["SuccessMessage"] = $"Password for '{user.Email}' reset successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var err in result.Errors)
        {
            ModelState.AddModelError(string.Empty, err.Description);
        }

        return View(model);
    }
}
