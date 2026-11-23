using DoctorApp.Data;
using DoctorApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Tests;

internal static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static Doctor NewDoctor(int slotMinutes = 20) => new()
    {
        Id = 1,
        FullName = "Тестовый врач",
        SpecialtyId = 1,
        Cabinet = "101",
        WorkStart = new TimeOnly(8, 0),
        WorkEnd = new TimeOnly(14, 0),
        SlotMinutes = slotMinutes,
        IsActive = true
    };

    public static DateTime NextWeekday(int fromDays = 1)
    {
        DateTime day = DateTime.Today.AddDays(fromDays);
        while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day;
    }

    public static DateTime TenthWorkingDay()
    {
        DateTime day = DateTime.Today;
        DateTime last = day;
        int count = 0;
        while (count < 10)
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                count++;
                last = day;
            }
            day = day.AddDays(1);
        }
        return last;
    }
}
