using System.Diagnostics;
using DoctorApp.Data;
using DoctorApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Controllers;

public class HomeController : AppController
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.DoctorCount = await _db.Doctors.CountAsync(d => d.IsActive);
        ViewBag.SpecialtyCount = await _db.Specialties.CountAsync();

        if (User.Identity?.IsAuthenticated == true && User.IsInRole(nameof(UserRole.Patient)))
        {
            int id = CurrentUserId;
            ViewBag.Next = await _db.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
                .Where(a => a.PatientId == id && a.Status == AppointmentStatus.Scheduled && a.StartTime > DateTime.Now)
                .OrderBy(a => a.StartTime)
                .FirstOrDefaultAsync();
        }

        if (User.Identity?.IsAuthenticated == true && !User.IsInRole(nameof(UserRole.Patient)))
        {
            DateTime today = DateTime.Today;
            ViewBag.TodayCount = await _db.Appointments
                .CountAsync(a => a.StartTime >= today && a.StartTime < today.AddDays(1) && a.Status != AppointmentStatus.Cancelled);
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
