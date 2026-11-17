using System.Text;
using DoctorApp.Data;
using DoctorApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Controllers;

[Authorize(Roles = "Admin,Registrar")]
public class ReportsController : AppController
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        return View(await BuildAsync(from, to));
    }

    public async Task<IActionResult> Csv(DateTime? from, DateTime? to)
    {
        var report = await BuildAsync(from, to);
        var sb = new StringBuilder();
        sb.AppendLine($"Отчёт по записям с {report.From:dd.MM.yyyy} по {report.To:dd.MM.yyyy}");
        sb.AppendLine("Врач;Всего;Запланировано;Проведено;Отменено;Неявки");
        foreach (var r in report.ByDoctor.Append(report.Total))
            sb.AppendLine($"{r.Name};{r.Total};{r.Scheduled};{r.Completed};{r.Cancelled};{r.NoShow}");

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"report_{report.From:yyyyMMdd}_{report.To:yyyyMMdd}.csv");
    }

    private async Task<ReportViewModel> BuildAsync(DateTime? from, DateTime? to)
    {
        DateTime start = (from ?? DateTime.Today.AddDays(-14)).Date;
        DateTime end = (to ?? DateTime.Today.AddDays(7)).Date;
        if (end < start)
            (start, end) = (end, start);

        var items = await _db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Where(a => a.StartTime >= start && a.StartTime < end.AddDays(1))
            .ToListAsync();

        static ReportRow Row(string name, IEnumerable<Appointment> list) => new()
        {
            Name = name,
            Total = list.Count(),
            Scheduled = list.Count(a => a.Status == AppointmentStatus.Scheduled),
            Completed = list.Count(a => a.Status == AppointmentStatus.Completed),
            Cancelled = list.Count(a => a.Status == AppointmentStatus.Cancelled),
            NoShow = list.Count(a => a.Status == AppointmentStatus.NoShow)
        };

        return new ReportViewModel
        {
            From = start,
            To = end,
            ByDoctor = items.GroupBy(a => a.Doctor!.FullName).Select(g => Row(g.Key, g)).OrderByDescending(r => r.Total).ToList(),
            BySpecialty = items.GroupBy(a => a.Doctor!.Specialty!.Name).Select(g => Row(g.Key, g)).OrderByDescending(r => r.Total).ToList(),
            Total = Row("Итого", items),
            ByDay = items.Where(a => a.Status != AppointmentStatus.Cancelled)
                .GroupBy(a => a.StartTime.Date).OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Count())).ToList()
        };
    }
}
