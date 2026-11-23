using DoctorApp.Models;
using DoctorApp.Services;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task CreateRemindersAsync_CalledTwice_CreatesOneNotification()
    {
        using var db = TestDb.Create();
        var patient = new User { Login = "p1", FullName = "Пациент", PasswordHash = "x", Role = UserRole.Patient };
        var doctor = new Doctor { FullName = "Иванова", Specialty = new Specialty { Name = "Терапевт" }, Cabinet = "101" };
        db.Appointments.Add(new Appointment { Doctor = doctor, Patient = patient, StartTime = DateTime.Now.AddHours(5) });
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        await service.CreateRemindersAsync(patient.Id);
        await service.CreateRemindersAsync(patient.Id);

        Assert.Equal(1, await db.Notifications.CountAsync());
    }

    [Fact]
    public async Task CreateRemindersAsync_AppointmentBeyondDay_CreatesNothing()
    {
        using var db = TestDb.Create();
        var patient = new User { Login = "p2", FullName = "Пациент", PasswordHash = "x", Role = UserRole.Patient };
        var doctor = new Doctor { FullName = "Иванова", Specialty = new Specialty { Name = "Терапевт" }, Cabinet = "101" };
        db.Appointments.Add(new Appointment { Doctor = doctor, Patient = patient, StartTime = DateTime.Now.AddDays(3) });
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        await service.CreateRemindersAsync(patient.Id);

        Assert.Equal(0, await db.Notifications.CountAsync());
    }

    [Fact]
    public async Task UnreadCountAsync_CountsOnlyUnreadOfUser()
    {
        using var db = TestDb.Create();
        db.Notifications.Add(new Notification { UserId = 1, Text = "a" });
        db.Notifications.Add(new Notification { UserId = 1, Text = "b", IsRead = true });
        db.Notifications.Add(new Notification { UserId = 2, Text = "c" });
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        Assert.Equal(1, await service.UnreadCountAsync(1));
    }
}
