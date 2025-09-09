using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;
using Inventory_Management_Requirements.Services;
using CloudinaryDotNet;
using Data;
using Inventory_Management_Requirements.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Add Razor Pages services

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<SignInManager<ApplicationUser>>();

// Configure Identity UI
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<ICustomIdGenerator, CustomIdGenerator>();
builder.Services.AddScoped<IFileStorageService, CloudinaryFileStorageService_Debug>();
builder.Services.AddScoped<SearchService>();

var app = builder.Build();

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

// Add Identity UI pages (login, register, etc.)
app.MapRazorPages();

// Seed database with roles, admin user, categories, and tags
await DbInitializer.Seed(app);
Console.WriteLine("Database seeded successfully with admin user and initial data.");

// Use a different port to avoid conflicts
app.Urls.Add("http://localhost:5006");
app.Urls.Add("http://0.0.0.0:5006");

Console.WriteLine("Application is running on http://localhost:5006");
Console.WriteLine("Login page available at: http://localhost:5006/Identity/Account/Login");
Console.WriteLine("Press Ctrl+C to stop the application");

app.Run();
