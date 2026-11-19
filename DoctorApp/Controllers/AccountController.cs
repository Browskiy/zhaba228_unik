using System.Security.Claims;
using DoctorApp.Data;
using DoctorApp.Models;
using DoctorApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Controllers;

public class AccountController : AppController
{
    private static readonly string[] AllowedAvatarExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxAvatarBytes = 2 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly NotificationService _notifications;
    private readonly IWebHostEnvironment _env;

    public AccountController(AppDbContext db, NotificationService notifications, IWebHostEnvironment env)
    {
        _db = db;
        _notifications = notifications;
        _env = env;
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

    [HttpGet, Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
            return RedirectToAction("Login");

        return View(BuildPage(user, new ProfileViewModel { FullName = user.FullName, Phone = user.Phone }, new ChangePasswordViewModel()));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> UpdateProfile(ProfileViewModel Info)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
            return RedirectToAction("Login");

        if (!ModelState.IsValid)
            return View("Profile", BuildPage(user, Info, new ChangePasswordViewModel()));

        user.FullName = Info.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(Info.Phone) ? null : Info.Phone.Trim();
        await _db.SaveChangesAsync();
        await SignInAsync(user);

        Success("Личные данные сохранены");
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel Password)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
            return RedirectToAction("Login");

        if (ModelState.IsValid && !PasswordHasher.Verify(Password.CurrentPassword, user.PasswordHash))
            ModelState.AddModelError("Password.CurrentPassword", "Текущий пароль указан неверно");

        if (!ModelState.IsValid)
            return View("Profile", BuildPage(user, new ProfileViewModel { FullName = user.FullName, Phone = user.Phone }, Password));

        user.PasswordHash = PasswordHasher.Hash(Password.NewPassword);
        await _db.SaveChangesAsync();

        Success("Пароль изменён");
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
            return RedirectToAction("Login");

        if (avatar == null || avatar.Length == 0)
        {
            Error("Выберите файл изображения");
            return RedirectToAction(nameof(Profile));
        }

        string extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
        if (!AllowedAvatarExtensions.Contains(extension) || !avatar.ContentType.StartsWith("image/"))
        {
            Error("Допустимы только файлы JPG, PNG или WEBP");
            return RedirectToAction(nameof(Profile));
        }

        if (avatar.Length > MaxAvatarBytes)
        {
            Error("Размер фото не должен превышать 2 МБ");
            return RedirectToAction(nameof(Profile));
        }

        string folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(folder);
        string fileName = $"{user.Id}_{Guid.NewGuid():N}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
        {
            await avatar.CopyToAsync(stream);
        }

        DeleteAvatarFile(user.AvatarPath);
        user.AvatarPath = "/uploads/avatars/" + fileName;
        await _db.SaveChangesAsync();
        await SignInAsync(user);

        Success("Фото обновлено");
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> RemoveAvatar()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
            return RedirectToAction("Login");

        DeleteAvatarFile(user.AvatarPath);
        user.AvatarPath = null;
        await _db.SaveChangesAsync();
        await SignInAsync(user);

        Success("Фото удалено");
        return RedirectToAction(nameof(Profile));
    }

    private ProfilePageViewModel BuildPage(User user, ProfileViewModel info, ChangePasswordViewModel password) => new()
    {
        Info = info,
        Password = password,
        Login = user.Login,
        Role = user.Role,
        AvatarPath = user.AvatarPath
    };

    private void DeleteAvatarFile(string? avatarPath)
    {
        if (string.IsNullOrEmpty(avatarPath))
            return;

        string fullPath = Path.Combine(_env.WebRootPath, avatarPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (!string.IsNullOrEmpty(user.AvatarPath))
            claims.Add(new Claim("AvatarPath", user.AvatarPath));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
