using System.ComponentModel.DataAnnotations;

namespace DoctorApp.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите логин")]
    [Display(Name = "Логин")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите ФИО")]
    [MaxLength(150)]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Введите логин")]
    [RegularExpression("^[a-zA-Z0-9_.]{3,50}$", ErrorMessage = "От 3 до 50 латинских букв, цифр, точек или подчёркиваний")]
    [Display(Name = "Логин")]
    public string Login { get; set; } = "";

    [Phone(ErrorMessage = "Некорректный телефон")]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Дата рождения")]
    public DateTime? BirthDate { get; set; }

    [Required(ErrorMessage = "Введите пароль")]
    [MinLength(6, ErrorMessage = "Не менее 6 символов")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = "";

    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    [DataType(DataType.Password)]
    [Display(Name = "Повтор пароля")]
    public string ConfirmPassword { get; set; } = "";
}

public class DoctorScheduleViewModel
{
    public Doctor Doctor { get; set; } = null!;
    public DateTime Date { get; set; }
    public List<DateTime> FreeSlots { get; set; } = new();
    public int? RescheduleId { get; set; }
    public Appointment? Rescheduling { get; set; }
    public List<User> Patients { get; set; } = new();
    public List<(DateTime Day, int Free)> Days { get; set; } = new();
}

public class ReportRow
{
    public string Name { get; set; } = "";
    public int Total { get; set; }
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int NoShow { get; set; }
}

public class ReportViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<ReportRow> ByDoctor { get; set; } = new();
    public List<ReportRow> BySpecialty { get; set; } = new();
    public ReportRow Total { get; set; } = new();
    public List<(DateTime Day, int Count)> ByDay { get; set; } = new();
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Введите ФИО")]
    [MaxLength(150)]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = "";

    [Phone(ErrorMessage = "Некорректный телефон")]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Введите текущий пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Текущий пароль")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Введите новый пароль")]
    [MinLength(6, ErrorMessage = "Не менее 6 символов")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; } = "";

    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    [DataType(DataType.Password)]
    [Display(Name = "Повтор нового пароля")]
    public string ConfirmPassword { get; set; } = "";
}

public class ProfilePageViewModel
{
    public ProfileViewModel Info { get; set; } = new();
    public ChangePasswordViewModel Password { get; set; } = new();
    public string Login { get; set; } = "";
    public UserRole Role { get; set; }
    public string? AvatarPath { get; set; }
}
