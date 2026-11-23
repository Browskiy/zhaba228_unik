using System.Text;
using DoctorApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoctorApp.Controllers;

[Authorize(Roles = "Admin,Registrar")]
public class ReportsController : AppController
{
    private readonly ReportService _reports;

    public ReportsController(ReportService reports)
    {
        _reports = reports;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        return View(await _reports.BuildAsync(from, to));
    }

    public async Task<IActionResult> Csv(DateTime? from, DateTime? to)
    {
        var report = await _reports.BuildAsync(from, to);
        var sb = new StringBuilder();
        sb.AppendLine($"Отчёт по записям с {report.From:dd.MM.yyyy} по {report.To:dd.MM.yyyy}");
        sb.AppendLine("Врач;Всего;Запланировано;Проведено;Отменено;Неявки");
        foreach (var r in report.ByDoctor.Append(report.Total))
            sb.AppendLine($"{r.Name};{r.Total};{r.Scheduled};{r.Completed};{r.Cancelled};{r.NoShow}");

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"report_{report.From:yyyyMMdd}_{report.To:yyyyMMdd}.csv");
    }
}
