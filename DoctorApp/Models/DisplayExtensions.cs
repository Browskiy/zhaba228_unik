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

    public static string Icon(this AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "bi-clock",
        AppointmentStatus.Completed => "bi-check2",
        AppointmentStatus.Cancelled => "bi-x",
        AppointmentStatus.NoShow => "bi-person-x",
        _ => "bi-dot"
    };

    public static string Initials(this string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(p => char.ToUpper(p[0])));
    }

    public static string AvatarClass(this string fullName) => $"av-{fullName.Select((c, i) => c * (i + 7)).Sum() % 6}";

    public static string RoleIcon(this UserRole role) => role switch
    {
        UserRole.Patient => "bi-person",
        UserRole.Registrar => "bi-clipboard2-pulse",
        UserRole.Doctor => "bi-heart-pulse",
        UserRole.Admin => "bi-shield-lock",
        _ => "bi-person"
    };
}
