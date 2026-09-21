using System.Security.Claims;
using Deployguard.Data;
using Deployguard.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<AppUser> _passwordHasher;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<AppUser>();
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Password is required.";
            return View();
        }

        var admin = await _context.AppUsers
            .FirstOrDefaultAsync(u =>
                u.Role == "Admin" &&
                u.IsActive);

        if (admin == null)
        {
            ViewBag.Error = "Admin account was not found.";
            return View();
        }

        var result = _passwordHasher.VerifyHashedPassword(
            admin,
            admin.PasswordHash,
            password);

        if (result == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "Invalid admin password.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.UserName),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return RedirectToAction(nameof(Dashboard));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Dashboard()
    {
        return View();
    }
}
