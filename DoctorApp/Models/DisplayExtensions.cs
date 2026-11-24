namespace DoctorApp.Models;

public static class DisplayExtensions
{
    public static string ToRussian(this UserRole role) => role switch
    {
        UserRole.Patient => "Пациент",
        UserRole.Registrar => "Регистратор",
        UserRole.Doctor => "Врач",
        UserRole.Admin => "Администратор",
        _ => role.ToString()
    };

    public static string ToRussian(this AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "Запланирован",
        AppointmentStatus.Completed => "Проведён",
        AppointmentStatus.Cancelled => "Отменён",
        AppointmentStatus.NoShow => "Неявка",
        _ => status.ToString()
    };

    public static string BadgeClass(this AppointmentStatus status) => $"pill st-{(int)status}";

    public static string Initials(this string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(p => char.ToUpper(p[0])));
    }

    public static string Plural(this int count, string one, string few, string many)
    {
        int tens = count % 100, units = count % 10;
        if (tens is >= 11 and <= 14) return many;
        return units == 1 ? one : units is >= 2 and <= 4 ? few : many;
    }
}
