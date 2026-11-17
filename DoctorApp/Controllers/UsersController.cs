using DoctorApp.Data;
using DoctorApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : AppController
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _db.Users.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int id, UserRole role)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();
        if (user.Id == CurrentUserId)
        {
            Error("Нельзя изменить роль своей учётной записи");
            return RedirectToAction(nameof(Index));
        }

        user.Role = role;
        await _db.SaveChangesAsync();
        Success($"Пользователю {user.FullName} назначена роль «{role.ToRussian()}»");
        return RedirectToAction(nameof(Index));
    }
}
