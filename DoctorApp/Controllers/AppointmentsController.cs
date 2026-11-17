using DoctorApp.Data;
using DoctorApp.Models;
using DoctorApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Controllers;

[Authorize]
public class AppointmentsController : AppController
{
    private const string Staff = "Admin,Registrar";
    private readonly AppDbContext _db;
    private readonly ScheduleService _schedule;
    private readonly NotificationService _notifications;

    public AppointmentsController(AppDbContext db, ScheduleService schedule, NotificationService notifications)
    {
        _db = db;
        _schedule = schedule;
        _notifications = notifications;
    }

    private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("Registrar");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(int doctorId, DateTime start, int? patientId)
    {
        var doctor = await _db.Doctors.Include(d => d.Specialty).FirstOrDefaultAsync(d => d.Id == doctorId);
        if (doctor == null)
            return NotFound();

        int patient = IsStaff && patientId.HasValue ? patientId.Value : CurrentUserId;
        if (!IsStaff && !User.IsInRole("Patient"))
            return Forbid();

        if (!await _schedule.IsSlotFreeAsync(doctor, start))
        {
            Error("Это время уже занято или недоступно. Выберите другое.");
            return RedirectToAction("Schedule", "Doctors", new { id = doctorId, date = start.Date });
        }

        if (await _db.Appointments.AnyAsync(a => a.PatientId == patient && a.StartTime == start && a.Status == AppointmentStatus.Scheduled))
        {
            Error("У пациента уже есть запись на это время к другому врачу.");
            return RedirectToAction("Schedule", "Doctors", new { id = doctorId, date = start.Date });
        }

        _db.Appointments.Add(new Appointment { DoctorId = doctorId, PatientId = patient, StartTime = start });
        _notifications.Add(patient, $"Вы записаны на приём {start:dd.MM.yyyy HH:mm}: {doctor.Specialty!.Name.ToLower()} {doctor.FullName}, кабинет {doctor.Cabinet}.");

        if (!await TrySaveAsync())
            return RedirectToAction("Schedule", "Doctors", new { id = doctorId, date = start.Date });

        Success($"Запись на {start:dd.MM.yyyy HH:mm} оформлена");
        return IsStaff ? RedirectToAction(nameof(All), new { date = start.Date }) : RedirectToAction(nameof(My));
    }

    public async Task<IActionResult> My()
    {
        int id = CurrentUserId;
        var list = await _db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Where(a => a.PatientId == id)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var a = await LoadAsync(id);
        if (a == null)
            return NotFound();
        if (!IsStaff && a.PatientId != CurrentUserId)
            return Forbid();
        if (a.Status != AppointmentStatus.Scheduled)
        {
            Error("Отменить можно только запланированную запись");
            return Back();
        }

        a.Status = AppointmentStatus.Cancelled;
        _notifications.Add(a.PatientId, $"Запись на {a.StartTime:dd.MM.yyyy HH:mm} к врачу {a.Doctor!.FullName} отменена.");
        await _db.SaveChangesAsync();
        Success("Запись отменена");
        return Back();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime start)
    {
        var a = await LoadAsync(id);
        if (a == null)
            return NotFound();
        if (!IsStaff && a.PatientId != CurrentUserId)
            return Forbid();
        if (a.Status != AppointmentStatus.Scheduled || !await _schedule.IsSlotFreeAsync(a.Doctor!, start, a.Id))
        {
            Error("Перенести запись на это время нельзя");
            return RedirectToAction("Schedule", "Doctors", new { id = a.DoctorId, date = start.Date, rescheduleId = id });
        }

        DateTime old = a.StartTime;
        a.StartTime = start;
        _notifications.Add(a.PatientId, $"Запись к врачу {a.Doctor!.FullName} перенесена с {old:dd.MM.yyyy HH:mm} на {start:dd.MM.yyyy HH:mm}.");

        if (!await TrySaveAsync())
            return RedirectToAction("Schedule", "Doctors", new { id = a.DoctorId, date = start.Date, rescheduleId = id });

        Success($"Запись перенесена на {start:dd.MM.yyyy HH:mm}");
        return IsStaff ? RedirectToAction(nameof(All), new { date = start.Date }) : RedirectToAction(nameof(My));
    }

    [Authorize(Roles = Staff)]
    public async Task<IActionResult> All(DateTime? date, int? doctorId)
    {
        DateTime day = (date ?? DateTime.Today).Date;
        var query = _db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Include(a => a.Patient)
            .Where(a => a.StartTime >= day && a.StartTime < day.AddDays(1));
        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId);

        ViewBag.Date = day;
        ViewBag.Doctors = new SelectList(await _db.Doctors.OrderBy(d => d.FullName).ToListAsync(), "Id", "FullName", doctorId);
        return View(await query.OrderBy(a => a.StartTime).ThenBy(a => a.Doctor!.FullName).ToListAsync());
    }

    [Authorize(Roles = "Admin,Registrar,Doctor")]
    public async Task<IActionResult> DoctorDay(DateTime? date)
    {
        DateTime day = (date ?? DateTime.Today).Date;
        int userId = CurrentUserId;
        var doctor = await _db.Doctors.Include(d => d.Specialty).FirstOrDefaultAsync(d => d.UserId == userId);
        if (doctor == null)
        {
            Error("Ваша учётная запись не привязана к врачу. Обратитесь к администратору.");
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Date = day;
        ViewBag.Doctor = doctor;
        var list = await _db.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctor.Id && a.StartTime >= day && a.StartTime < day.AddDays(1) && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Registrar,Doctor")]
    public async Task<IActionResult> SetStatus(int id, AppointmentStatus status)
    {
        var a = await LoadAsync(id);
        if (a == null)
            return NotFound();
        if (User.IsInRole("Doctor") && a.Doctor!.UserId != CurrentUserId)
            return Forbid();
        if (status == AppointmentStatus.Scheduled)
            return BadRequest();

        a.Status = status;
        if (status == AppointmentStatus.NoShow)
            _notifications.Add(a.PatientId, $"Отмечена неявка на приём {a.StartTime:dd.MM.yyyy HH:mm}. Если вы не можете прийти, отменяйте запись заранее.");
        await _db.SaveChangesAsync();
        Success($"Статус записи изменён: {status.ToRussian()}");
        return Back();
    }

    private Task<Appointment?> LoadAsync(int id)
    {
        return _db.Appointments.Include(a => a.Doctor).FirstOrDefaultAsync(a => a.Id == id);
    }

    private async Task<bool> TrySaveAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            Error("Это время только что заняли. Выберите другое.");
            return false;
        }
    }

    private IActionResult Back()
    {
        string? referer = Request.Headers.Referer;
        return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction(IsStaff ? nameof(All) : nameof(My));
    }
}
