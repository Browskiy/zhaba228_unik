using DoctorApp.Data;
using DoctorApp.Diagnostics;
using DoctorApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NLog.Web;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--bench"))
{
    await PerformanceBenchmark.RunAsync(builder.Configuration.GetConnectionString("DefaultConnection")!);
    return;
}

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (args.Contains("--sql"))
    {
        Console.WriteLine(db.Database.GenerateCreateScript());
        return;
    }

    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS \"AvatarPath\" character varying(255)");
    db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_appointments_StartTime\" ON appointments (\"StartTime\")");
    DbSeeder.Seed(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Logger.LogInformation("DoctorApp запущен, окружение {Environment}", app.Environment.EnvironmentName);
app.Run();
