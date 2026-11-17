using System.Security.Claims;
using DoctorApp.Data;
using DoctorApp.Models;
using DoctorApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Controllers;

public class AccountController : AppController
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifications;

    public AccountController(AppDbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == model.Login.Trim());
        if (user == null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Неверный логин или пароль");
            return View(model);
        }

        await SignInAsync(user);

        if (user.Role == UserRole.Patient)
            await _notifications.CreateRemindersAsync(user.Id);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (await _db.Users.AnyAsync(u => u.Login == model.Login))
            ModelState.AddModelError(nameof(model.Login), "Такой логин уже занят");

        if (!ModelState.IsValid)
            return View(model);

        var user = new User
        {
            Login = model.Login.Trim(),
            FullName = model.FullName.Trim(),
            Phone = model.Phone,
            BirthDate = model.BirthDate,
            Role = UserRole.Patient,
            PasswordHash = PasswordHasher.Hash(model.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _notifications.Add(user.Id, "Добро пожаловать в электронную регистратуру! Теперь вы можете записаться на приём.");
        await _db.SaveChangesAsync();

        await SignInAsync(user);
        Success("Регистрация прошла успешно");
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
