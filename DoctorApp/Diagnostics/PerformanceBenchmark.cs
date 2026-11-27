using System.Diagnostics;
using System.Text;
using DoctorApp.Data;
using DoctorApp.Models;
using DoctorApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace DoctorApp.Diagnostics;

public static class PerformanceBenchmark
{
    private const int Warmup = 3;
    private const int Runs = 15;

    public static async Task RunAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "doctorapp_bench" };
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(builder.ConnectionString).Options;
        var output = new StringBuilder();

        void Print(string line)
        {
            Console.WriteLine(line);
            output.AppendLine(line);
        }

        await using var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        var timer = Stopwatch.StartNew();
        await SeedAsync(db);
        Print($"Тестовые данные: врачей {await db.Doctors.CountAsync()}, пациентов {await db.Users.CountAsync()}, записей {await db.Appointments.CountAsync()} (создано за {timer.Elapsed.TotalSeconds:F1} с)");
        Print($"Каждый замер: {Warmup} прогрева, затем среднее по {Runs} запускам");
        Print("");

        var doctor = await db.Doctors.AsNoTracking().OrderBy(d => d.Id).FirstAsync();
        var schedule = new ScheduleService(db, NullLogger<ScheduleService>.Instance);

        double before = await MeasureAsync(() => LegacyDaysAsync(db, doctor));
        double after = await MeasureAsync(() => schedule.FreeCountsAsync(doctor, 10));
        Print($"1. Расписание врача на 10 рабочих дней: до {before:F2} мс, после {after:F2} мс, ускорение в {before / after:F1} раза");

        var patient = await db.Users.AsNoTracking().FirstAsync(u => u.Login == "bench_patient");
        var notifications = new NotificationService(db);
        await notifications.CreateRemindersAsync(patient.Id);
        db.ChangeTracker.Clear();
        before = await MeasureAsync(() => LegacyRemindersAsync(db, patient.Id));
        after = await MeasureAsync(() => notifications.CreateRemindersAsync(patient.Id));
        Print($"2. Проверка напоминаний пациента с 20 ближайшими записями: до {before:F2} мс, после {after:F2} мс, ускорение в {before / after:F1} раза");

        DateTime from = DateTime.Today.AddDays(-90);
        DateTime to = DateTime.Today;
        var reports = new ReportService(db);

        await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS \"IX_appointments_StartTime\"");
        await db.Database.ExecuteSqlRawAsync("ANALYZE appointments");
        before = await MeasureAsync(() => LegacyReportAsync(db, from, to));
        double withoutIndex = await MeasureAsync(() => reports.BuildAsync(from, to));
        await db.Database.ExecuteSqlRawAsync("CREATE INDEX \"IX_appointments_StartTime\" ON appointments (\"StartTime\")");
        await db.Database.ExecuteSqlRawAsync("ANALYZE appointments");
        after = await MeasureAsync(() => reports.BuildAsync(from, to));
        Print($"3. Отчёт за 90 дней: до {before:F2} мс; проекция и AsNoTracking без индекса {withoutIndex:F2} мс; вместе с индексом по StartTime {after:F2} мс; общее ускорение в {before / after:F1} раза");

        DateTime probeDay = DateTime.Today.AddDays(1);
        while (probeDay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            probeDay = probeDay.AddDays(1);
        DateTime probe = probeDay + doctor.WorkEnd.ToTimeSpan() - TimeSpan.FromMinutes(doctor.SlotMinutes);
        before = await MeasureAsync(() => { for (int i = 0; i < 20000; i++) _ = ScheduleService.AllSlots(doctor, probe.Date).Contains(probe); return Task.CompletedTask; });
        after = await MeasureAsync(() => { for (int i = 0; i < 20000; i++) _ = ScheduleService.IsSlotTime(doctor, probe); return Task.CompletedTask; });
        Print($"4. Проверка принадлежности времени сетке слотов (20 000 проверок): до {before:F2} мс, после {after:F2} мс, ускорение в {before / after:F1} раза");

        var cache = new MemoryCache(new MemoryCacheOptions());
        before = await MeasureAsync(async () => _ = await db.Specialties.AsNoTracking().OrderBy(s => s.Name).ToListAsync());
        after = await MeasureAsync(async () => _ = await cache.GetOrCreateAsync("specialties", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await db.Specialties.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        }));
        Print($"5. Список специальностей: запрос к базе {before:F3} мс, из кэша {after:F4} мс, ускорение в {before / after:F0} раз");

        var rows = await db.Appointments.AsNoTracking().CountAsync(a => a.StartTime >= from && a.StartTime < to.AddDays(1));
        Print($"   (в отчёт попало записей: {rows})");

        await db.Database.EnsureDeletedAsync();
        string path = Path.Combine(AppContext.BaseDirectory, "benchmark.txt");
        await File.WriteAllTextAsync(path, output.ToString(), Encoding.UTF8);
        Console.WriteLine($"Результаты сохранены: {path}");
    }

    private static async Task<double> MeasureAsync(Func<Task> action)
    {
        for (int i = 0; i < Warmup; i++)
            await action();

        var timer = Stopwatch.StartNew();
        for (int i = 0; i < Runs; i++)
            await action();
        timer.Stop();
        return timer.Elapsed.TotalMilliseconds / Runs;
    }

    private static async Task LegacyDaysAsync(AppDbContext db, Doctor doctor)
    {
        var days = new List<(DateTime Day, int Free)>();
        DateTime d = DateTime.Today;
        while (days.Count < 10)
        {
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                DateTime dayStart = d.Date;
                DateTime dayEnd = dayStart.AddDays(1);
                var busy = await db.Appointments
                    .Where(a => a.DoctorId == doctor.Id
                                && a.Status == AppointmentStatus.Scheduled
                                && a.StartTime >= dayStart && a.StartTime < dayEnd)
                    .Select(a => a.StartTime)
                    .ToListAsync();
                DateTime now = DateTime.Now;
                int free = ScheduleService.AllSlots(doctor, d).Count(s => s > now && !busy.Contains(s));
                days.Add((d, free));
            }
            d = d.AddDays(1);
        }
    }

    private static async Task LegacyRemindersAsync(AppDbContext db, int userId)
    {
        DateTime now = DateTime.Now;
        DateTime limit = now.AddHours(24);
        var upcoming = await db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Where(a => a.PatientId == userId
                        && a.Status == AppointmentStatus.Scheduled
                        && a.StartTime > now && a.StartTime <= limit)
            .ToListAsync();

        foreach (var a in upcoming)
        {
            string text = $"Напоминание: приём {a.StartTime:dd.MM.yyyy HH:mm}, {a.Doctor!.Specialty!.Name.ToLower()} {a.Doctor.FullName}, кабинет {a.Doctor.Cabinet}.";
            bool exists = await db.Notifications.AnyAsync(n => n.UserId == userId && n.Text == text);
            if (!exists)
                db.Notifications.Add(new Notification { UserId = userId, Text = text });
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static async Task LegacyReportAsync(AppDbContext db, DateTime from, DateTime to)
    {
        DateTime start = from.Date;
        DateTime end = to.Date;
        var items = await db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Where(a => a.StartTime >= start && a.StartTime < end.AddDays(1))
            .ToListAsync();

        _ = items.GroupBy(a => a.Doctor!.FullName).Select(g => g.Count(a => a.Status == AppointmentStatus.Completed)).ToList();
        _ = items.GroupBy(a => a.Doctor!.Specialty!.Name).Select(g => g.Count(a => a.Status == AppointmentStatus.Cancelled)).ToList();
        _ = items.Where(a => a.Status != AppointmentStatus.Cancelled).GroupBy(a => a.StartTime.Date).Select(g => g.Count()).ToList();
        db.ChangeTracker.Clear();
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        var specialties = Enumerable.Range(1, 8).Select(i => new Specialty { Name = $"Специальность {i}" }).ToList();
        db.Specialties.AddRange(specialties);

        var patients = Enumerable.Range(1, 2000)
            .Select(i => new User { Login = $"bench{i}", FullName = $"Пациент {i}", PasswordHash = "x", Role = UserRole.Patient })
            .ToList();
        var special = new User { Login = "bench_patient", FullName = "Пациент с напоминаниями", PasswordHash = "x", Role = UserRole.Patient };
        db.Users.AddRange(patients);
        db.Users.Add(special);

        var doctors = Enumerable.Range(1, 40)
            .Select(i => new Doctor
            {
                FullName = $"Врач {i}",
                Specialty = specialties[i % specialties.Count],
                Cabinet = (100 + i).ToString(),
                SlotMinutes = 15,
                WorkStart = new TimeOnly(8, 0),
                WorkEnd = new TimeOnly(16, 0)
            })
            .ToList();
        db.Doctors.AddRange(doctors);
        await db.SaveChangesAsync();

        var random = new Random(11);
        DateTime now = DateTime.Now;
        int batch = 0;
        for (DateTime day = DateTime.Today.AddDays(-120); day <= DateTime.Today.AddDays(30); day = day.AddDays(1))
        {
            foreach (var doctor in doctors)
            {
                foreach (var slot in ScheduleService.AllSlots(doctor, day).Where(_ => random.NextDouble() < 0.6))
                {
                    var status = slot < now
                        ? (random.NextDouble() switch { < 0.75 => AppointmentStatus.Completed, < 0.9 => AppointmentStatus.NoShow, _ => AppointmentStatus.Cancelled })
                        : AppointmentStatus.Scheduled;
                    db.Appointments.Add(new Appointment
                    {
                        DoctorId = doctor.Id,
                        PatientId = patients[random.Next(patients.Count)].Id,
                        StartTime = slot,
                        Status = status,
                        CreatedAt = slot.AddDays(-3)
                    });
                    batch++;
                }
            }

            if (batch >= 5000)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                batch = 0;
            }
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var servingDoctors = doctors.Take(20).ToList();
        for (int i = 0; i < servingDoctors.Count; i++)
        {
            db.Appointments.Add(new Appointment
            {
                DoctorId = servingDoctors[i].Id,
                PatientId = special.Id,
                StartTime = now.AddHours(2).Date.AddHours(now.AddHours(2).Hour).AddMinutes(i + 1).AddSeconds(30),
                Status = AppointmentStatus.Scheduled,
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }
}
