using System.Diagnostics;
using DoctorApp.Data;
using DoctorApp.Models;
using DoctorApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DoctorApp.Controllers;

public class DoctorsController : AppController
{
    private const string Staff = "Admin,Registrar";
    private readonly AppDbContext _db;
    private readonly ScheduleService _schedule;
    private readonly ILogger<DoctorsController> _logger;
    private readonly IMemoryCache _cache;

    public DoctorsController(AppDbContext db, ScheduleService schedule, ILogger<DoctorsController> logger, IMemoryCache cache)
    {
        _db = db;
        _schedule = schedule;
        _logger = logger;
        _cache = cache;
    }

    private async Task<List<Specialty>> SpecialtiesAsync()
    {
        var list = await _cache.GetOrCreateAsync("specialties", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _db.Specialties.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        });
        return list!;
    }

    public async Task<IActionResult> Index(int? specialtyId, string? search)
    {
        var query = _db.Doctors.AsNoTracking().Include(d => d.Specialty).AsQueryable();

        if (!User.IsInRole("Admin") && !User.IsInRole("Registrar"))
            query = query.Where(d => d.IsActive);
        if (specialtyId.HasValue)
            query = query.Where(d => d.SpecialtyId == specialtyId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => EF.Functions.ILike(d.FullName, $"%{search.Trim()}%"));

        ViewBag.SpecialtyList = await SpecialtiesAsync();
        ViewBag.SpecialtyId = specialtyId;
        ViewBag.Search = search;
        var doctors = await query.OrderBy(d => d.Specialty!.Name).ThenBy(d => d.FullName).ToListAsync();
        ViewBag.Nearest = await _schedule.NearestFreeSlotsAsync(doctors);
        return View(doctors);
    }

    [Authorize]
    public async Task<IActionResult> Schedule(int id, DateTime? date, int? rescheduleId)
    {
        var timer = Stopwatch.StartNew();
        var doctor = await _db.Doctors.AsNoTracking().Include(d => d.Specialty).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null)
            return NotFound();

        DayOfWeek weekday = DateTime.Today.DayOfWeek;
        int restOfWeek = weekday is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0 : DayOfWeek.Saturday - weekday;
        var days = await _schedule.FreeCountsAsync(doctor, restOfWeek + 10);

        DateTime day = date?.Date ?? days.Where(d => d.Free > 0).Select(d => d.Day).DefaultIfEmpty(DateTime.Today).First();
        var model = new DoctorScheduleViewModel
        {
            Doctor = doctor,
            Date = day,
            Days = days,
            FreeSlots = doctor.IsActive ? await _schedule.FreeSlotsAsync(doctor, day) : new(),
            RescheduleId = rescheduleId
        };

        if (rescheduleId.HasValue)
            model.Rescheduling = await _db.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == rescheduleId);

        if (User.IsInRole("Patient"))
        {
            int me = CurrentUserId;
            var mine = await _db.Appointments
                .AsNoTracking()
                .Where(a => a.PatientId == me
                            && a.Status == AppointmentStatus.Scheduled
                            && a.StartTime >= day && a.StartTime < day.AddDays(1)
                            && a.Id != rescheduleId)
                .Select(a => a.StartTime)
                .ToListAsync();
            model.PatientBusy = mine.ToHashSet();
        }

        if (User.IsInRole("Admin") || User.IsInRole("Registrar"))
            model.Patients = await _db.Users.AsNoTracking().Where(u => u.Role == UserRole.Patient).OrderBy(u => u.FullName).ToListAsync();

        _logger.LogInformation("Расписание врача {DoctorId} на {Date:dd.MM.yyyy} построено за {Elapsed} мс", id, day, timer.ElapsedMilliseconds);
        return View(model);
    }

    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Create()
    {
        await FillListsAsync(null);
        return View("Edit", new Doctor());
    }

    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Edit(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor == null)
            return NotFound();
        await FillListsAsync(doctor);
        return View(doctor);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Staff)]
    public async Task<IActionResult> Save(Doctor doctor)
    {
        ModelState.Remove(nameof(Doctor.Specialty));
        ModelState.Remove(nameof(Doctor.User));
        if (doctor.WorkEnd <= doctor.WorkStart)
            ModelState.AddModelError(nameof(Doctor.WorkEnd), "Конец приёма должен быть позже начала");

        if (!ModelState.IsValid)
        {
            await FillListsAsync(doctor);
            return View("Edit", doctor);
        }

        if (doctor.Id == 0)
            _db.Doctors.Add(doctor);
        else
            _db.Doctors.Update(doctor);

        await _db.SaveChangesAsync();
        Success($"Врач «{doctor.FullName}» сохранён");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Staff)]
    public async Task<IActionResult> Delete(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor == null)
            return NotFound();

        if (await _db.Appointments.AnyAsync(a => a.DoctorId == id))
        {
            doctor.IsActive = false;
            Success($"У врача есть история приёмов, поэтому он не удалён, а снят с приёма");
        }
        else
        {
            _db.Doctors.Remove(doctor);
            Success($"Врач «{doctor.FullName}» удалён");
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task FillListsAsync(Doctor? doctor)
    {
        ViewBag.Specialties = new SelectList(await _db.Specialties.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", doctor?.SpecialtyId);
        ViewBag.DoctorUsers = new SelectList(await _db.Users.Where(u => u.Role == UserRole.Doctor).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName", doctor?.UserId);
    }
}
