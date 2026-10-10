using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatientAdmissionPortal.Models;

namespace PatientAdmissionPortal.Data;

public class DefaultUserSettings
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "Default User";
}

public static class DefaultUserSeeder
{
    public static void Seed(DbContext context, DefaultUserSettings settings)
    {
        // nothing configured: do nothing
        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
            return;

        var email = settings.Email.Trim();
        var normalized = email.ToUpperInvariant();
        var users = context.Set<ApplicationUser>();

        if (users.Any(u => u.NormalizedEmail == normalized))
            return; // already exists, so this is safe to run many times

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = normalized,
            Email = email,
            NormalizedEmail = normalized,
            EmailConfirmed = true,               // no verification code needed for the seeded user
            FullName = settings.FullName,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, settings.Password);

        users.Add(user);
        context.SaveChanges();
    }
}