using DoctorApp.Models;
using DoctorApp.Services;

namespace DoctorApp.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any())
            return;

        var admin = new User { Login = "admin", FullName = "Администратор системы", Role = UserRole.Admin, PasswordHash = PasswordHasher.Hash("admin123") };
        var registrar = new User { Login = "registrar", FullName = "Соколова Анна Викторовна", Role = UserRole.Registrar, PasswordHash = PasswordHasher.Hash("registrar123") };
        var doctorUser = new User { Login = "ivanova", FullName = "Иванова Мария Петровна", Role = UserRole.Doctor, PasswordHash = PasswordHasher.Hash("doctor123") };

        var patients = new[]
        {
            new User { Login = "petrov", FullName = "Петров Сергей Иванович", Phone = "+79001234567", BirthDate = new DateTime(1985, 3, 12), Role = UserRole.Patient, PasswordHash = PasswordHasher.Hash("patient123") },
            new User { Login = "smirnova", FullName = "Смирнова Ольга Андреевна", Phone = "+79007654321", BirthDate = new DateTime(1992, 7, 5), Role = UserRole.Patient, PasswordHash = PasswordHasher.Hash("patient123") },
            new User { Login = "kuznetsov", FullName = "Кузнецов Дмитрий Олегович", Phone = "+79005553311", BirthDate = new DateTime(1978, 11, 23), Role = UserRole.Patient, PasswordHash = PasswordHasher.Hash("patient123") }
        };

        db.Users.AddRange(admin, registrar, doctorUser);
        db.Users.AddRange(patients);

        var specialties = new[] { "Терапевт", "Хирург", "Офтальмолог", "Невролог", "Педиатр" }
            .Select(n => new Specialty { Name = n }).ToArray();
        db.Specialties.AddRange(specialties);

        var doctors = new[]
        {
            new Doctor { FullName = "Иванова Мария Петровна", Specialty = specialties[0], Cabinet = "101", User = doctorUser },
            new Doctor { FullName = "Козлов Андрей Николаевич", Specialty = specialties[0], Cabinet = "102", WorkStart = new TimeOnly(14, 0), WorkEnd = new TimeOnly(19, 0) },
            new Doctor { FullName = "Орлов Виктор Семёнович", Specialty = specialties[1], Cabinet = "205", SlotMinutes = 30 },
            new Doctor { FullName = "Белова Елена Игоревна", Specialty = specialties[2], Cabinet = "310", WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(15, 0) },
            new Doctor { FullName = "Фёдоров Павел Юрьевич", Specialty = specialties[3], Cabinet = "214", SlotMinutes = 30 },
            new Doctor { FullName = "Новикова Ирина Алексеевна", Specialty = specialties[4], Cabinet = "120" }
        };
        db.Doctors.AddRange(doctors);
        db.SaveChanges();

        var random = new Random(7);
        DateTime day = DateTime.Today.AddDays(-14);
        while (day < DateTime.Today.AddDays(7))
        {
            foreach (var doctor in doctors)
            {
                var slots = ScheduleService.AllSlots(doctor, day);
                foreach (var slot in slots.Where(_ => random.NextDouble() < 0.25))
                {
                    var status = slot < DateTime.Now
                        ? (random.NextDouble() switch { < 0.75 => AppointmentStatus.Completed, < 0.9 => AppointmentStatus.NoShow, _ => AppointmentStatus.Cancelled })
                        : (random.NextDouble() < 0.9 ? AppointmentStatus.Scheduled : AppointmentStatus.Cancelled);

                    db.Appointments.Add(new Appointment
                    {
                        DoctorId = doctor.Id,
                        PatientId = patients[random.Next(patients.Length)].Id,
                        StartTime = slot,
                        Status = status,
                        CreatedAt = slot.AddDays(-random.Next(1, 10))
                    });
                }
            }
            day = day.AddDays(1);
        }

        db.SaveChanges();
    }
}
