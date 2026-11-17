using DoctorApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Controllers;

[Authorize]
public class NotificationsController : AppController
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        int id = CurrentUserId;
        var list = await _db.Notifications
            .Where(n => n.UserId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        int id = CurrentUserId;
        await _db.Notifications
            .Where(n => n.UserId == id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return RedirectToAction(nameof(Index));
    }
}
