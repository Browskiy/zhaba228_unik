using DoctorApp.Data;
using DoctorApp.Models;
using DoctorApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Controllers;

public class DoctorsController : AppController
{
    private const string Staff = "Admin,Registrar";
    private readonly AppDbContext _db;
    private readonly ScheduleService _schedule;

    public DoctorsController(AppDbContext db, ScheduleService schedule)
    {
        _db = db;
        _schedule = schedule;
    }

    public async Task<IActionResult> Index(int? specialtyId, string? search)
    {
        var query = _db.Doctors.Include(d => d.Specialty).AsQueryable();

        if (!User.IsInRole("Admin") && !User.IsInRole("Registrar"))
            query = query.Where(d => d.IsActive);
        if (specialtyId.HasValue)
            query = query.Where(d => d.SpecialtyId == specialtyId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => EF.Functions.ILike(d.FullName, $"%{search.Trim()}%"));

        ViewBag.Specialties = new SelectList(await _db.Specialties.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", specialtyId);
        ViewBag.Search = search;
        return View(await query.OrderBy(d => d.Specialty!.Name).ThenBy(d => d.FullName).ToListAsync());
    }

    [Authorize]
    public async Task<IActionResult> Schedule(int id, DateTime? date, int? rescheduleId)
    {
        var doctor = await _db.Doctors.Include(d => d.Specialty).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null)
            return NotFound();

        DateTime day = (date ?? DateTime.Today).Date;
        var model = new DoctorScheduleViewModel
        {
            Doctor = doctor,
            Date = day,
            FreeSlots = doctor.IsActive ? await _schedule.FreeSlotsAsync(doctor, day) : new(),
            RescheduleId = rescheduleId
        };

        if (User.IsInRole("Admin") || User.IsInRole("Registrar"))
            model.Patients = await _db.Users.Where(u => u.Role == UserRole.Patient).OrderBy(u => u.FullName).ToListAsync();

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
