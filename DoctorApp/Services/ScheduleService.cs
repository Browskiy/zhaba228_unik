using DoctorApp.Data;
using DoctorApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Services;

public class ScheduleService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(AppDbContext db, ILogger<ScheduleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public static List<DateTime> AllSlots(Doctor doctor, DateTime date)
    {
        if (doctor.SlotMinutes <= 0
            || doctor.WorkEnd <= doctor.WorkStart
            || date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return new List<DateTime>();

        int count = (int)((doctor.WorkEnd - doctor.WorkStart).TotalMinutes / doctor.SlotMinutes);
        var slots = new List<DateTime>(count);
        DateTime time = date.Date + doctor.WorkStart.ToTimeSpan();
        DateTime end = date.Date + doctor.WorkEnd.ToTimeSpan();
        while (time.AddMinutes(doctor.SlotMinutes) <= end)
        {
            slots.Add(time);
            time = time.AddMinutes(doctor.SlotMinutes);
        }
        return slots;
    }

    public static bool IsSlotTime(Doctor doctor, DateTime start)
    {
        if (doctor.SlotMinutes <= 0
            || doctor.WorkEnd <= doctor.WorkStart
            || start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        TimeSpan slot = TimeSpan.FromMinutes(doctor.SlotMinutes);
        TimeSpan offset = start.TimeOfDay - doctor.WorkStart.ToTimeSpan();
        return offset >= TimeSpan.Zero
            && offset.Ticks % slot.Ticks == 0
            && start.TimeOfDay + slot <= doctor.WorkEnd.ToTimeSpan();
    }

    public async Task<List<DateTime>> FreeSlotsAsync(Doctor doctor, DateTime date)
    {
        if (doctor.SlotMinutes <= 0)
            _logger.LogWarning("У врача {DoctorId} задана некорректная длительность приёма: {Minutes}", doctor.Id, doctor.SlotMinutes);

        var busy = await BusyTimesAsync(doctor.Id, date.Date, date.Date.AddDays(1));
        DateTime now = DateTime.Now;
        return AllSlots(doctor, date)
            .Where(s => s > now && !busy.Contains(s))
            .ToList();
    }

    public async Task<List<(DateTime Day, int Free)>> FreeCountsAsync(Doctor doctor, int workingDays)
    {
        var days = new List<DateTime>();
        DateTime d = DateTime.Today;
        while (days.Count < workingDays)
        {
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days.Add(d);
            d = d.AddDays(1);
        }

        if (!doctor.IsActive || days.Count == 0)
            return days.Select(day => (day, 0)).ToList();

        var busy = await BusyTimesAsync(doctor.Id, days[0], days[^1].AddDays(1));
        DateTime now = DateTime.Now;
        return days
            .Select(day => (day, AllSlots(doctor, day).Count(s => s > now && !busy.Contains(s))))
            .ToList();
    }

    public async Task<Dictionary<int, DateTime>> NearestFreeSlotsAsync(IEnumerable<Doctor> doctors, int days = 21)
    {
        var active = doctors.Where(d => d.IsActive).ToList();
        var nearest = new Dictionary<int, DateTime>();
        if (active.Count == 0)
            return nearest;

        DateTime from = DateTime.Today;
        DateTime to = from.AddDays(days);
        var ids = active.Select(d => d.Id).ToList();
        var busy = (await _db.Appointments
                .AsNoTracking()
                .Where(a => ids.Contains(a.DoctorId)
                            && a.Status == AppointmentStatus.Scheduled
                            && a.StartTime >= from && a.StartTime < to)
                .Select(a => new { a.DoctorId, a.StartTime })
                .ToListAsync())
            .ToLookup(a => a.DoctorId, a => a.StartTime);

        DateTime now = DateTime.Now;
        foreach (var doctor in active)
        {
            var taken = busy[doctor.Id].ToHashSet();
            for (DateTime day = from; day < to; day = day.AddDays(1))
            {
                var free = AllSlots(doctor, day).Where(s => s > now && !taken.Contains(s)).Take(1).ToList();
                if (free.Count > 0)
                {
                    nearest[doctor.Id] = free[0];
                    break;
                }
            }
        }
        return nearest;
    }

    public async Task<bool> IsSlotFreeAsync(Doctor doctor, DateTime start, int? ignoreAppointmentId = null)
    {
        if (!doctor.IsActive || start <= DateTime.Now || !IsSlotTime(doctor, start))
            return false;

        return !await _db.Appointments.AnyAsync(a =>
            a.DoctorId == doctor.Id
            && a.StartTime == start
            && a.Status == AppointmentStatus.Scheduled
            && a.Id != ignoreAppointmentId);
    }

    private async Task<HashSet<DateTime>> BusyTimesAsync(int doctorId, DateTime from, DateTime to)
    {
        var times = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId
                        && a.Status == AppointmentStatus.Scheduled
                        && a.StartTime >= from && a.StartTime < to)
            .Select(a => a.StartTime)
            .ToListAsync();
        return new HashSet<DateTime>(times);
    }
}
