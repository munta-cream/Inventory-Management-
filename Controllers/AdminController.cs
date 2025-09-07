using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;
using System.Threading.Tasks;

namespace Inventory_Management_Requirements.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public IActionResult Index()
        {
            var users = _db.Users.AsNoTracking().ToList();
            return View(users);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user, string password)
        {
            if (ModelState.IsValid)
            {
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(user);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByIdAsync(user.Id);
                if (existingUser == null)
                {
                    return NotFound();
                }

                bool wasAdmin = existingUser.IsAdmin;

                // Update user properties
                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.IsAdmin = user.IsAdmin;
                existingUser.IsBlocked = user.IsBlocked;

                // Update user using UserManager
                var result = await _userManager.UpdateAsync(existingUser);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(user);
                }

                // Update role if IsAdmin changed
                if (wasAdmin != user.IsAdmin)
                {
                    if (user.IsAdmin)
                    {
                        // Add to Admin role using UserManager
                        var roleResult = await _userManager.AddToRoleAsync(existingUser, "Admin");
                        if (!roleResult.Succeeded)
                        {
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            return View(user);
                        }
                    }
                    else
                    {
                        // Remove from Admin role using UserManager
                        var roleResult = await _userManager.RemoveFromRoleAsync(existingUser, "Admin");
                        if (!roleResult.Succeeded)
                        {
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            return View(user);
                        }
                    }
                }

                return RedirectToAction("Index");
            }
            return View(user);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = !user.IsBlocked;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            bool wasAdmin = user.IsAdmin;
            user.IsAdmin = !user.IsAdmin;

            // Update role if IsAdmin changed
            if (wasAdmin != user.IsAdmin)
            {
                if (user.IsAdmin)
                {
                    // Add to Admin role using UserManager
                    var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    // Remove from Admin role using UserManager
                    var roleResult = await _userManager.RemoveFromRoleAsync(user, "Admin");
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return RedirectToAction("Index");
                    }
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        public IActionResult Statistics()
        {
            var stats = new
            {
                TotalUsers = _db.Users.Count(),
                TotalInventories = _db.Inventories.Count(),
                TotalItems = _db.Items.Count(),
                TotalCategories = _db.Categories.Count(),
                BlockedUsers = _db.Users.Count(u => u.IsBlocked),
                AdminUsers = _db.Users.Count(u => u.IsAdmin)
            };

            return View(stats);
        }
    }
}
