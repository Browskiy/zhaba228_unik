using DoctorApp.Data;
using DoctorApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Services;

public class ReportService
{
    private sealed record Item(string Doctor, string Specialty, AppointmentStatus Status, DateTime StartTime);

    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportViewModel> BuildAsync(DateTime? from, DateTime? to)
    {
        DateTime start = (from ?? DateTime.Today.AddDays(-14)).Date;
        DateTime end = (to ?? DateTime.Today.AddDays(7)).Date;
        if (end < start)
            (start, end) = (end, start);

        DateTime limit = end.AddDays(1);
        var items = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.StartTime >= start && a.StartTime < limit)
            .Select(a => new Item(a.Doctor!.FullName, a.Doctor.Specialty!.Name, a.Status, a.StartTime))
            .ToListAsync();

        return new ReportViewModel
        {
            From = start,
            To = end,
            ByDoctor = items.GroupBy(a => a.Doctor).Select(g => Row(g.Key, g)).OrderByDescending(r => r.Total).ToList(),
            BySpecialty = items.GroupBy(a => a.Specialty).Select(g => Row(g.Key, g)).OrderByDescending(r => r.Total).ToList(),
            Total = Row("Итого", items),
            ByDay = items.Where(a => a.Status != AppointmentStatus.Cancelled)
                .GroupBy(a => a.StartTime.Date).OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Count())).ToList()
        };
    }

    private static ReportRow Row(string name, IEnumerable<Item> list)
    {
        var rows = list.ToList();
        return new ReportRow
        {
            Name = name,
            Total = rows.Count,
            Scheduled = rows.Count(a => a.Status == AppointmentStatus.Scheduled),
            Completed = rows.Count(a => a.Status == AppointmentStatus.Completed),
            Cancelled = rows.Count(a => a.Status == AppointmentStatus.Cancelled),
            NoShow = rows.Count(a => a.Status == AppointmentStatus.NoShow)
        };
    }
}
