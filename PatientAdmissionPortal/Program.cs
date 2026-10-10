using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatientAdmissionPortal.Data;
using PatientAdmissionPortal.Models;
using PatientAdmissionPortal.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var defaultUser = builder.Configuration.GetSection("DefaultUser").Get<DefaultUserSettings>()
                  ?? new DefaultUserSettings();

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseSeeding((context, _) => DefaultUserSeeder.Seed(context, defaultUser))
    .UseAsyncSeeding((context, _, _) =>
    {
        DefaultUserSeeder.Seed(context, defaultUser);
        return Task.CompletedTask;
    }));
builder.Services.AddScoped<AdmissionStore>(); // scoped: one DbContext per request

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:Host"]))
    builder.Services.AddScoped<IAppEmailSender, SmtpEmailSender>();
else
    builder.Services.AddScoped<IAppEmailSender, ConsoleEmailSender>();
builder.Services.AddScoped<EmailVerificationService>();

// --- Identity (register / login with email) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Every page requires login unless marked [AllowAnonymous] (the Account pages are)
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

// Apply pending migrations and seed sample data on startup (fine for a prototype)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Admissions/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Use "Admissions" instead of "Home" if you haven't added the Home page yet
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
