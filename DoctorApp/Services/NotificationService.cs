using DoctorApp.Data;
using DoctorApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Services;

public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public void Add(int userId, string text)
    {
        _db.Notifications.Add(new Notification { UserId = userId, Text = text });
    }

    public async Task CreateRemindersAsync(int userId)
    {
        DateTime now = DateTime.Now;
        DateTime limit = now.AddHours(24);

        var upcoming = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Where(a => a.PatientId == userId
                        && a.Status == AppointmentStatus.Scheduled
                        && a.StartTime > now && a.StartTime <= limit)
            .ToListAsync();

        if (upcoming.Count == 0)
            return;

        var texts = upcoming
            .Select(a => $"Напоминание: приём {a.StartTime:dd.MM.yyyy HH:mm}, {a.Doctor!.Specialty!.Name.ToLower()} {a.Doctor.FullName}, кабинет {a.Doctor.Cabinet}.")
            .ToList();

        var existing = await _db.Notifications
            .Where(n => n.UserId == userId && texts.Contains(n.Text))
            .Select(n => n.Text)
            .ToListAsync();

        var known = new HashSet<string>(existing);
        foreach (string text in texts)
        {
            if (known.Add(text))
                Add(userId, text);
        }

        await _db.SaveChangesAsync();
    }

    public Task<int> UnreadCountAsync(int userId)
    {
        return _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }
}
