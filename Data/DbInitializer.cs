using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Models;

namespace Inventory_Management_Requirements.Data
{
    public static class DbInitializer
    {
        public static async Task Seed(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed default admin user
            var adminEmail = "admin@inventory.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    IsAdmin = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed categories
            if (!context.Categories.Any())
            {
                var categories = new[]
                {
                    new Category { Name = "Office Equipment", Description = "Office supplies and equipment" },
                    new Category { Name = "Books", Description = "Books and publications" },
                    new Category { Name = "Documents", Description = "Important documents and files" },
                    new Category { Name = "Electronics", Description = "Electronic devices and accessories" },
                    new Category { Name = "Furniture", Description = "Office and home furniture" },
                    new Category { Name = "Supplies", Description = "General supplies and materials" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // Seed some default tags
            if (!context.Tags.Any())
            {
                var tags = new[]
                {
                    new Tag { Name = "important" },
                    new Tag { Name = "archived" },
                    new Tag { Name = "active" },
                    new Tag { Name = "reviewed" },
                    new Tag { Name = "pending" }
                };
                await context.Tags.AddRangeAsync(tags);
                await context.SaveChangesAsync();
            }
        }
    }
}
