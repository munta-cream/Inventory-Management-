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

            
            await context.Database.EnsureCreatedAsync();

           
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
            if (adminUser != null)
            {
                // Delete existing user to ensure clean recreation
                await userManager.DeleteAsync(adminUser);
            }

            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                IsAdmin = true,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

           
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
