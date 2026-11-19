using System.ComponentModel.DataAnnotations;

namespace DoctorApp.Models;

public enum UserRole
{
    Patient = 0,
    Registrar = 1,
    Doctor = 2,
    Admin = 3
}

public enum AppointmentStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    NoShow = 3
}

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Login { get; set; } = "";

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = "";

    [Required, MaxLength(150)]
    public string FullName { get; set; } = "";

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateTime? BirthDate { get; set; }

    [MaxLength(255)]
    public string? AvatarPath { get; set; }

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Appointment> Appointments { get; set; } = new();
}

public class Specialty
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    public List<Doctor> Doctors { get; set; } = new();
}

public class Doctor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите ФИО"), MaxLength(150)]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = "";

    [Display(Name = "Специальность")]
    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    [Required(ErrorMessage = "Укажите кабинет"), MaxLength(20)]
    [Display(Name = "Кабинет")]
    public string Cabinet { get; set; } = "";

    [Display(Name = "Начало приёма")]
    public TimeOnly WorkStart { get; set; } = new(8, 0);

    [Display(Name = "Конец приёма")]
    public TimeOnly WorkEnd { get; set; } = new(14, 0);

    [Range(10, 120, ErrorMessage = "От 10 до 120 минут")]
    [Display(Name = "Длительность приёма, мин")]
    public int SlotMinutes { get; set; } = 20;

    [Display(Name = "Учётная запись врача")]
    public int? UserId { get; set; }
    public User? User { get; set; }

    [Display(Name = "Ведёт приём")]
    public bool IsActive { get; set; } = true;

    public List<Appointment> Appointments { get; set; } = new();
}

public class Appointment
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public int PatientId { get; set; }
    public User? Patient { get; set; }

    public DateTime StartTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    [MaxLength(300)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(300)]
    public string Text { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsRead { get; set; }
}
