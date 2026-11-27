using DoctorApp.Models;
using DoctorApp.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DoctorApp.Tests;

public class ScheduleServiceTests
{
    [Fact]
    public void AllSlots_Weekday_ReturnsAllSlotsOfWorkingDay()
    {
        DateTime day = TestDb.NextWeekday();

        var slots = ScheduleService.AllSlots(TestDb.NewDoctor(), day);

        Assert.Equal(18, slots.Count);
        Assert.Equal(day.AddHours(8), slots[0]);
        Assert.Equal(day.AddHours(13).AddMinutes(40), slots[^1]);
    }

    [Fact]
    public void AllSlots_Weekend_ReturnsEmptyList()
    {
        DateTime saturday = TestDb.NextWeekday();
        while (saturday.DayOfWeek != DayOfWeek.Saturday)
            saturday = saturday.AddDays(1);

        Assert.Empty(ScheduleService.AllSlots(TestDb.NewDoctor(), saturday));
    }

    [Fact(Timeout = 3000)]
    public async Task AllSlots_ZeroSlotMinutes_DoesNotHangAndReturnsEmptyList()
    {
        var slots = await Task.Run(() => ScheduleService.AllSlots(TestDb.NewDoctor(0), TestDb.NextWeekday()));

        Assert.Empty(slots);
    }

    [Fact]
    public void AllSlots_WorkEndBeforeStart_ReturnsEmptyList()
    {
        var doctor = TestDb.NewDoctor();
        doctor.WorkEnd = new TimeOnly(7, 0);

        Assert.Empty(ScheduleService.AllSlots(doctor, TestDb.NextWeekday()));
    }

    [Fact]
    public async Task IsSlotFreeAsync_FreeSlot_ReturnsTrue()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        DateTime start = ScheduleService.AllSlots(doctor, TestDb.NextWeekday())[3];

        Assert.True(await service.IsSlotFreeAsync(doctor, start));
    }

    [Fact]
    public async Task IsSlotFreeAsync_BusySlot_ReturnsFalse()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        DateTime start = ScheduleService.AllSlots(doctor, TestDb.NextWeekday())[3];
        db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 10, StartTime = start });
        await db.SaveChangesAsync();

        Assert.False(await service.IsSlotFreeAsync(doctor, start));
    }

    [Fact]
    public async Task IsSlotFreeAsync_CancelledAppointment_ReleasesSlot()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        DateTime start = ScheduleService.AllSlots(doctor, TestDb.NextWeekday())[3];
        db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 10, StartTime = start, Status = AppointmentStatus.Cancelled });
        await db.SaveChangesAsync();

        Assert.True(await service.IsSlotFreeAsync(doctor, start));
    }

    [Fact]
    public async Task IsSlotFreeAsync_OwnAppointmentIgnoredOnReschedule_ReturnsTrue()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        DateTime start = ScheduleService.AllSlots(doctor, TestDb.NextWeekday())[3];
        var own = new Appointment { DoctorId = doctor.Id, PatientId = 10, StartTime = start };
        db.Appointments.Add(own);
        await db.SaveChangesAsync();

        Assert.True(await service.IsSlotFreeAsync(doctor, start, own.Id));
    }

    [Fact]
    public async Task IsSlotFreeAsync_PastTime_ReturnsFalse()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);

        Assert.False(await service.IsSlotFreeAsync(TestDb.NewDoctor(), DateTime.Now.AddDays(-1)));
    }

    [Fact]
    public async Task IsSlotFreeAsync_TimeOutsideGrid_ReturnsFalse()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        DateTime start = ScheduleService.AllSlots(doctor, TestDb.NextWeekday())[3].AddMinutes(5);

        Assert.False(await service.IsSlotFreeAsync(doctor, start));
    }

    [Fact]
    public async Task IsSlotFreeAsync_InactiveDoctor_ReturnsFalse()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        doctor.IsActive = false;
        DateTime start = ScheduleService.AllSlots(doctor, TestDb.NextWeekday())[3];

        Assert.False(await service.IsSlotFreeAsync(doctor, start));
    }

    [Fact]
    public async Task FreeSlotsAsync_ExcludesBusySlots()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        DateTime day = TestDb.NextWeekday();
        DateTime busy = ScheduleService.AllSlots(doctor, day)[1];
        db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 10, StartTime = busy });
        await db.SaveChangesAsync();

        var free = await service.FreeSlotsAsync(doctor, day);

        Assert.Equal(17, free.Count);
        Assert.DoesNotContain(busy, free);
    }

    [Fact]
    public async Task FreeCountsAsync_ReturnsTenWorkingDaysWithBusySlotsExcluded()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        DateTime lastDay = TestDb.TenthWorkingDay();
        db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 10, StartTime = ScheduleService.AllSlots(doctor, lastDay)[0] });
        await db.SaveChangesAsync();

        var days = await service.FreeCountsAsync(doctor, 10);

        Assert.Equal(10, days.Count);
        Assert.All(days, d => Assert.False(d.Day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
        Assert.Equal(17, days.First(d => d.Day == lastDay).Free);
    }

    [Fact]
    public async Task FreeCountsAsync_InactiveDoctor_ReturnsZeroForEveryDay()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        doctor.IsActive = false;

        var days = await service.FreeCountsAsync(doctor, 10);

        Assert.Equal(10, days.Count);
        Assert.All(days, d => Assert.Equal(0, d.Free));
    }

    [Fact]
    public void IsSlotTime_AgreesWithAllSlotsForEveryMinuteOfDay()
    {
        var doctor = TestDb.NewDoctor();
        DateTime day = TestDb.NextWeekday();
        var slots = ScheduleService.AllSlots(doctor, day).ToHashSet();

        for (int minute = 0; minute < 24 * 60; minute++)
        {
            DateTime probe = day.AddMinutes(minute);
            Assert.Equal(slots.Contains(probe), ScheduleService.IsSlotTime(doctor, probe));
        }
    }

    [Fact]
    public void IsSlotTime_WithSecondsOrWeekendOrZeroSlot_ReturnsFalse()
    {
        var doctor = TestDb.NewDoctor();
        DateTime day = TestDb.NextWeekday();
        DateTime saturday = day;
        while (saturday.DayOfWeek != DayOfWeek.Saturday)
            saturday = saturday.AddDays(1);

        Assert.False(ScheduleService.IsSlotTime(doctor, day.AddHours(8).AddSeconds(1)));
        Assert.False(ScheduleService.IsSlotTime(doctor, saturday.AddHours(8)));
        Assert.False(ScheduleService.IsSlotTime(TestDb.NewDoctor(0), day.AddHours(8)));
    }

    [Fact]
    public async Task NearestFreeSlotsAsync_SkipsBusySlotsButNotCancelled()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        DateTime day = TestDb.NextWeekday();
        foreach (DateTime slot in ScheduleService.AllSlots(doctor, DateTime.Today))
            db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 10, StartTime = slot });
        var slots = ScheduleService.AllSlots(doctor, day);
        db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 10, StartTime = slots[0] });
        db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 11, StartTime = slots[1] });
        db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 12, StartTime = slots[2], Status = AppointmentStatus.Cancelled });
        await db.SaveChangesAsync();

        var nearest = await service.NearestFreeSlotsAsync(new[] { doctor });

        Assert.Equal(slots[2], nearest[doctor.Id]);
    }

    [Fact]
    public async Task NearestFreeSlotsAsync_InactiveDoctor_IsNotIncluded()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        doctor.IsActive = false;

        var nearest = await service.NearestFreeSlotsAsync(new[] { doctor });

        Assert.Empty(nearest);
    }

    [Fact]
    public async Task NearestFreeSlotsAsync_NoFreeTimeInRange_IsNotIncluded()
    {
        using var db = TestDb.Create();
        var service = new ScheduleService(db, NullLogger<ScheduleService>.Instance);
        var doctor = TestDb.NewDoctor();
        foreach (DateTime slot in ScheduleService.AllSlots(doctor, DateTime.Today))
            db.Appointments.Add(new Appointment { DoctorId = doctor.Id, PatientId = 10, StartTime = slot });
        await db.SaveChangesAsync();

        var nearest = await service.NearestFreeSlotsAsync(new[] { doctor }, days: 1);

        Assert.Empty(nearest);
    }
}
