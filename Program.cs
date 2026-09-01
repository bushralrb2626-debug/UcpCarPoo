using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UcpCarPool.Data;
using UcpCarPool.Models;
using UcpCarPool.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// MVC
// =========================================================

builder.Services.AddControllersWithViews();


// =========================================================
// HTTP CLIENT
// =========================================================

builder.Services.AddHttpClient();


// =========================================================
// EMAIL SERVICE
// =========================================================

builder.Services.AddScoped<EmailService>();


// =========================================================
// DATABASE
// =========================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));


// =========================================================
// IDENTITY
// =========================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// =========================================================
// BUILD APP
// =========================================================

var app = builder.Build();


// =========================================================
// ERROR HANDLING
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


// =========================================================
// MIDDLEWARE
// =========================================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();


// =========================================================
// ROUTING
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


// =========================================================
// SEED DATA
// =========================================================

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(
        scope.ServiceProvider);
}


// =========================================================
// RUN
// =========================================================

app.Run();