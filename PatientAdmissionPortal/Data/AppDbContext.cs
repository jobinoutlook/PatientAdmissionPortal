using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PatientAdmissionPortal.Models;

namespace PatientAdmissionPortal.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // required for Identity tables

        modelBuilder.Entity<ApplicationUser>(e => e.Property(u => u.FullName).HasMaxLength(100));

        modelBuilder.Entity<EmailVerificationCode>(e =>
        {
            e.ToTable("EmailVerificationCodes");
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.CodeHash).HasMaxLength(64);
            e.HasIndex(c => c.Email);
        });

        modelBuilder.Entity<Admission>(e =>
        {
            e.ToTable("Admissions");
            e.Ignore(a => a.Age); // computed, not stored

            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);

            e.Property(a => a.FullName).HasMaxLength(100);
            e.Property(a => a.Gender).HasMaxLength(20);
            e.Property(a => a.Phone).HasMaxLength(30);
            e.Property(a => a.Email).HasMaxLength(150);
            e.Property(a => a.EmergencyContactName).HasMaxLength(100);
            e.Property(a => a.EmergencyContactPhone).HasMaxLength(30);
            e.Property(a => a.Department).HasMaxLength(100);
            e.Property(a => a.Reason).HasMaxLength(500);
            e.Property(a => a.InsuranceProvider).HasMaxLength(100);
            e.Property(a => a.InsuranceId).HasMaxLength(50);
            e.Property(a => a.Bed).HasMaxLength(50);

            e.Property(a => a.DateOfBirth).HasColumnType("date");

            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.FullName);
        });
    }
}
