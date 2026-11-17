using DoctorApp.Data;
using DoctorApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Services;

public class ScheduleService
{
    private readonly AppDbContext _db;

    public ScheduleService(AppDbContext db)
    {
        _db = db;
    }

    public static List<DateTime> AllSlots(Doctor doctor, DateTime date)
    {
        var slots = new List<DateTime>();
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return slots;

        DateTime time = date.Date + doctor.WorkStart.ToTimeSpan();
        DateTime end = date.Date + doctor.WorkEnd.ToTimeSpan();
        while (time.AddMinutes(doctor.SlotMinutes) <= end)
        {
            slots.Add(time);
            time = time.AddMinutes(doctor.SlotMinutes);
        }
        return slots;
    }

    public async Task<List<DateTime>> FreeSlotsAsync(Doctor doctor, DateTime date)
    {
        DateTime dayStart = date.Date;
        DateTime dayEnd = dayStart.AddDays(1);

        var busy = await _db.Appointments
            .Where(a => a.DoctorId == doctor.Id
                        && a.Status == AppointmentStatus.Scheduled
                        && a.StartTime >= dayStart && a.StartTime < dayEnd)
            .Select(a => a.StartTime)
            .ToListAsync();

        DateTime now = DateTime.Now;
        return AllSlots(doctor, date)
            .Where(s => s > now && !busy.Contains(s))
            .ToList();
    }

    public async Task<bool> IsSlotFreeAsync(Doctor doctor, DateTime start, int? ignoreAppointmentId = null)
    {
        if (!doctor.IsActive || start <= DateTime.Now || !AllSlots(doctor, start.Date).Contains(start))
            return false;

        return !await _db.Appointments.AnyAsync(a =>
            a.DoctorId == doctor.Id
            && a.StartTime == start
            && a.Status == AppointmentStatus.Scheduled
            && a.Id != ignoreAppointmentId);
    }
}
