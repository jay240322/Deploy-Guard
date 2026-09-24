using System.Security.Claims;
using Deployguard.Data;
using Deployguard.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<AppUser> _passwordHasher;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<AppUser>();
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Email and password are required.";
            return View();
        }

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !user.IsActive)
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string username,
        string email,
        string password,
        string fullName)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fullName))
        {
            ViewBag.Error = "All fields are required.";
            return View();
        }

        if (await _context.AppUsers.AnyAsync(u => u.Email == email))
        {
            ViewBag.Error = "An account with this email already exists.";
            return View();
        }

        if (await _context.AppUsers.AnyAsync(u => u.UserName == username))
        {
            ViewBag.Error = "This username is already taken.";
            return View();
        }

        var user = new AppUser
        {
            UserName = username,
            Email = email,
            FullName = fullName,
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return Json(new { success = false, message = "Email is required for Google sign-in." });
        }

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            var baseUsername = request.Email.Split('@')[0].Replace(".", "_").ToLower();
            var username = baseUsername;
            int count = 1;
            while (await _context.AppUsers.AnyAsync(u => u.UserName == username))
            {
                username = $"{baseUsername}{count++}";
            }

            user = new AppUser
            {
                UserName = username,
                Email = request.Email,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? username : request.FullName,
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();
        }

        if (!user.IsActive)
        {
            return Json(new { success = false, message = "This account is inactive. Please contact an administrator." });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

public class GoogleLoginRequest
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? GoogleUid { get; set; }
}