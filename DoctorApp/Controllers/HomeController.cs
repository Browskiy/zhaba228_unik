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
        ViewBag.Specialties = await _db.Specialties.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync();

        if (User.Identity?.IsAuthenticated != true)
            return View("Landing");

        DateTime today = DateTime.Today;
        DateTime tomorrow = today.AddDays(1);

        if (User.IsInRole(nameof(UserRole.Patient)))
        {
            int id = CurrentUserId;
            var upcoming = await _db.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
                .Where(a => a.PatientId == id && a.Status == AppointmentStatus.Scheduled && a.StartTime > DateTime.Now)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
            ViewBag.Next = upcoming.FirstOrDefault();
            ViewBag.UpcomingCount = upcoming.Count;
            ViewBag.VisitCount = await _db.Appointments.CountAsync(a => a.PatientId == id && a.Status == AppointmentStatus.Completed);
            return View("Patient");
        }

        IQueryable<Appointment> todayQuery = _db.Appointments
            .Where(a => a.StartTime >= today && a.StartTime < tomorrow && a.Status != AppointmentStatus.Cancelled);

        if (User.IsInRole(nameof(UserRole.Doctor)) && !User.IsInRole(nameof(UserRole.Admin)))
        {
            int userId = CurrentUserId;
            var doctor = await _db.Doctors.Include(d => d.Specialty).FirstOrDefaultAsync(d => d.UserId == userId);
            ViewBag.Doctor = doctor;
            if (doctor != null)
            {
                var list = await todayQuery.Include(a => a.Patient).Where(a => a.DoctorId == doctor.Id).OrderBy(a => a.StartTime).ToListAsync();
                ViewBag.Today = list;
            }
            return View("Doctor");
        }

        var todayList = await todayQuery.ToListAsync();
        ViewBag.TodayTotal = todayList.Count;
        ViewBag.TodayScheduled = todayList.Count(a => a.Status == AppointmentStatus.Scheduled);
        ViewBag.TodayCompleted = todayList.Count(a => a.Status == AppointmentStatus.Completed);
        ViewBag.TodayNoShow = todayList.Count(a => a.Status == AppointmentStatus.NoShow);
        return View("Staff");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
