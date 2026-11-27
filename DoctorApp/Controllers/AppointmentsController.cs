using DoctorApp.Data;
using DoctorApp.Models;
using DoctorApp.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DoctorApp.Controllers;

[Authorize]
public class AppointmentsController : AppController
{
    private const string Staff = "Admin,Registrar";
    private readonly AppDbContext _db;
    private readonly ScheduleService _schedule;
    private readonly NotificationService _notifications;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(AppDbContext db, ScheduleService schedule, NotificationService notifications, ILogger<AppointmentsController> logger)
    {
        _db = db;
        _schedule = schedule;
        _notifications = notifications;
        _logger = logger;
    }

    private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("Registrar");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(int doctorId, DateTime start, int? patientId)
    {
        var doctor = await _db.Doctors.Include(d => d.Specialty).FirstOrDefaultAsync(d => d.Id == doctorId);
        if (doctor == null)
            return NotFound();

        if (!IsStaff && !User.IsInRole("Patient"))
            return Forbid();

        int patient = IsStaff ? patientId ?? 0 : CurrentUserId;
        if (!await PatientExistsAsync(patient))
        {
            _logger.LogWarning("Запись отклонена: пациент {PatientId} не найден", patient);
            Error("Выберите пациента из списка.");
            return RedirectToAction("Schedule", "Doctors", new { id = doctorId, date = DayParam(start) });
        }

        if (!await _schedule.IsSlotFreeAsync(doctor, start))
        {
            _logger.LogInformation("Запись отклонена: время {Start} врача {DoctorId} занято или недоступно", start, doctorId);
            Error("Это время уже занято или недоступно. Выберите другое.");
            return RedirectToAction("Schedule", "Doctors", new { id = doctorId, date = DayParam(start) });
        }

        if (await PatientBusyAsync(patient, start, null))
        {
            _logger.LogInformation("Запись отклонена: у пациента {PatientId} уже есть запись на {Start}", patient, start);
            Error("У пациента уже есть запись на это время к другому врачу.");
            return RedirectToAction("Schedule", "Doctors", new { id = doctorId, date = DayParam(start) });
        }

        var appointment = new Appointment { DoctorId = doctorId, PatientId = patient, StartTime = start };
        _db.Appointments.Add(appointment);
        _notifications.Add(patient, $"Вы записаны на приём {start:dd.MM.yyyy HH:mm}: {doctor.Specialty!.Name.ToLower()} {doctor.FullName}, кабинет {doctor.Cabinet}.");

        if (!await TrySaveAsync())
            return RedirectToAction("Schedule", "Doctors", new { id = doctorId, date = DayParam(start) });

        _logger.LogInformation("Создана запись на {Start} к врачу {DoctorId} для пациента {PatientId}", start, doctorId, patient);
        TempData["Booked"] = "new";
        return RedirectToAction(nameof(Ticket), new { id = appointment.Id });
    }

    public async Task<IActionResult> Ticket(int id)
    {
        var a = await _db.Appointments
            .AsNoTracking()
            .Include(x => x.Doctor).ThenInclude(d => d!.Specialty)
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a == null)
            return NotFound();
        if (!IsStaff && a.PatientId != CurrentUserId && a.Doctor!.UserId != CurrentUserId)
            return Forbid();
        return View(a);
    }

    public async Task<IActionResult> My()
    {
        int id = CurrentUserId;
        var list = await _db.Appointments
            .AsNoTracking()
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
        _logger.LogInformation("Запись {AppointmentId} отменена пользователем {UserId}", a.Id, CurrentUserId);
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
            _logger.LogInformation("Перенос записи {AppointmentId} на {Start} отклонён: время недоступно", id, start);
            Error("Перенести запись на это время нельзя");
            return RedirectToAction("Schedule", "Doctors", new { id = a.DoctorId, date = DayParam(start), rescheduleId = id });
        }

        if (await PatientBusyAsync(a.PatientId, start, a.Id))
        {
            _logger.LogInformation("Перенос записи {AppointmentId} отклонён: у пациента уже есть запись на {Start}", id, start);
            Error("У пациента уже есть запись на это время к другому врачу.");
            return RedirectToAction("Schedule", "Doctors", new { id = a.DoctorId, date = DayParam(start), rescheduleId = id });
        }

        DateTime old = a.StartTime;
        a.StartTime = start;
        _notifications.Add(a.PatientId, $"Запись к врачу {a.Doctor!.FullName} перенесена с {old:dd.MM.yyyy HH:mm} на {start:dd.MM.yyyy HH:mm}.");

        if (!await TrySaveAsync())
            return RedirectToAction("Schedule", "Doctors", new { id = a.DoctorId, date = DayParam(start), rescheduleId = id });

        _logger.LogInformation("Запись {AppointmentId} перенесена с {Old} на {Start}", id, old, start);
        TempData["Booked"] = "moved";
        return RedirectToAction(nameof(Ticket), new { id });
    }

    [Authorize(Roles = Staff)]
    public async Task<IActionResult> All(DateTime? date, int? doctorId)
    {
        DateTime day = (date ?? DateTime.Today).Date;
        var query = _db.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Include(a => a.Patient)
            .Where(a => a.StartTime >= day && a.StartTime < day.AddDays(1));
        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId);

        ViewBag.Date = day;
        ViewBag.Doctors = new SelectList(await _db.Doctors.AsNoTracking().OrderBy(d => d.FullName).ToListAsync(), "Id", "FullName", doctorId);
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
            .AsNoTracking()
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
        if (!Enum.IsDefined(status) || status == AppointmentStatus.Scheduled)
        {
            _logger.LogWarning("Недопустимый статус {Status} для записи {AppointmentId}", status, id);
            return BadRequest();
        }

        a.Status = status;
        if (status == AppointmentStatus.NoShow)
            _notifications.Add(a.PatientId, $"Отмечена неявка на приём {a.StartTime:dd.MM.yyyy HH:mm}. Если вы не можете прийти, отменяйте запись заранее.");
        await _db.SaveChangesAsync();
        _logger.LogInformation("Статус записи {AppointmentId} изменён на {Status}", id, status);
        Success($"Статус записи изменён: {status.ToRussian()}");
        return Back();
    }

    private static string DayParam(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _logger.LogWarning(ex, "Конфликт записи: время уже занято другим пациентом");
            Error("Это время только что заняли. Выберите другое.");
            return false;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Не удалось сохранить запись");
            Error("Не удалось сохранить запись. Попробуйте позже.");
            return false;
        }
    }

    private Task<bool> PatientExistsAsync(int patientId)
    {
        return _db.Users.AnyAsync(u => u.Id == patientId && u.Role == UserRole.Patient);
    }

    private Task<bool> PatientBusyAsync(int patientId, DateTime start, int? ignoreAppointmentId)
    {
        return _db.Appointments.AnyAsync(x =>
            x.PatientId == patientId
            && x.StartTime == start
            && x.Status == AppointmentStatus.Scheduled
            && x.Id != ignoreAppointmentId);
    }

    private IActionResult Back()
    {
        string? referer = Request.Headers.Referer;
        return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction(IsStaff ? nameof(All) : nameof(My));
    }
}
