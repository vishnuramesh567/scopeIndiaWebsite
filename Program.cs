using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ScopeIndiaWebsite.Data;
using ScopeIndiaWebsite.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(6);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<MVCDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MVCConnection")));


// 🔥 Cookie Authentication added
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Student/Login";
        options.LogoutPath = "/Student/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Keep me logged in
    });

var app = builder.Build();

// Seed courses if not exists
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MVCDbContext>();

    if (!context.Courses.Any())
    {
        context.Courses.AddRange(
            new Course { CourseName = "ASP.NET Core", Duration = "6 Months", Fee = 50000 },
            new Course { CourseName = "Python", Duration = "5 Months", Fee = 30500 },
            new Course { CourseName = "Data Science", Duration = "7 Months", Fee = 25000 },
            new Course { CourseName = "Digital Marketing Pro", Duration = "3 Months", Fee = 22500 },
            new Course { CourseName = "Cloud Computing & DevOps", Duration = "5 Months", Fee = 41000 },
            new Course { CourseName = "Full Stack Web Development", Duration = "8 Months", Fee = 55000 },
            new Course { CourseName = "Mobile App Development", Duration = "7 Months", Fee = 48000 },
            new Course { CourseName = "Cybersecurity Expert", Duration = "9 Months", Fee = 62000 },
            new Course { CourseName = "Java", Duration = "7 Months", Fee = 60000 }
        );
        context.SaveChanges();
    }
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔥 Authentication must come before Authorization
app.UseAuthentication();

app.UseSession();

// 🔹 Auto restore session from cookie
app.Use(async (context, next) =>
{
    if (context.Session.GetString("StudentId") == null)
    {
        if (context.Request.Cookies.TryGetValue("StudentCookie", out var studentIdFromCookie))
        {
            context.Session.SetString("StudentId", studentIdFromCookie);
        }
    }
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
