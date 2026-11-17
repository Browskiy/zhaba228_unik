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

    public static string BadgeClass(this AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "bg-primary",
        AppointmentStatus.Completed => "bg-success",
        AppointmentStatus.Cancelled => "bg-secondary",
        AppointmentStatus.NoShow => "bg-danger",
        _ => "bg-light"
    };
}
