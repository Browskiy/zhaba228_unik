using DoctorApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().ToTable("users");
        b.Entity<User>().HasIndex(u => u.Login).IsUnique();

        b.Entity<Specialty>().ToTable("specialties");
        b.Entity<Specialty>().HasIndex(s => s.Name).IsUnique();

        b.Entity<Doctor>().ToTable("doctors");
        b.Entity<Doctor>()
            .HasOne(d => d.Specialty).WithMany(s => s.Doctors)
            .HasForeignKey(d => d.SpecialtyId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Doctor>()
            .HasOne(d => d.User).WithMany()
            .HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<Appointment>().ToTable("appointments");
        b.Entity<Appointment>()
            .HasOne(a => a.Doctor).WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>()
            .HasOne(a => a.Patient).WithMany(u => u.Appointments)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.StartTime })
            .IsUnique()
            .HasFilter("\"Status\" = 0");
        b.Entity<Appointment>().HasIndex(a => a.StartTime);

        b.Entity<Notification>().ToTable("notifications");
        b.Entity<Notification>()
            .HasOne(n => n.User).WithMany()
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
