using System.Diagnostics;
using System.Security.Claims;
using Inventory_Management_Requirements.Models;
using Inventory_Management_Requirements.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Inventory_Management_Requirements.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // User not found, treat as unauthenticated
                    return RedirectToAction("Index", "Home");
                }
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                if (isAdmin)
                {
                    // Admin dashboard
                    var adminStats = new
                    {
                        TotalUsers = _db.Users.Count(),
                        TotalInventories = _db.Inventories.Count(),
                        TotalItems = _db.Items.Count(),
                        RecentInventories = await _db.Inventories
                            .Include(i => i.CreatedBy)
                            .Include(i => i.Category)
                            .OrderByDescending(i => i.CreatedAt)
                            .Take(5)
                            .ToListAsync(),
                        RecentUsers = await _db.Users
                            .OrderByDescending(u => u.Id) // Changed from CreatedAt to Id as CreatedAt does not exist
                            .Take(5)
                            .ToListAsync()
                    };

                    ViewBag.IsAdmin = true;
                    return View("AdminDashboard", adminStats);
                }
                else
                {
                    // Regular user dashboard
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var myInventories = await _db.Inventories
                        .Where(i => i.CreatedById == userId)
                        .Include(i => i.Category)
                        .Include(i => i.Items)
                        .ToListAsync();
                    var sharedInventories = await _db.InventoryAccesses
                        .Where(ia => ia.UserId == userId)
                        .Include(ia => ia.Inventory)
                        .ThenInclude(i => i.CreatedBy)
                        .Include(ia => ia.Inventory.Category)
                        .Select(ia => ia.Inventory)
                        .ToListAsync();
                    var recentActivity = await _db.Items
                        .Where(i => i.CreatedById == userId)
                        .Include(i => i.Inventory)
                        .OrderByDescending(i => i.CreatedAt)
                        .Take(10)
                        .ToListAsync();

                    var userStats = new
                    {
                        MyInventories = myInventories,
                        SharedInventories = sharedInventories,
                        RecentActivity = recentActivity,
                        TotalItems = myInventories.Sum(i => i.Items.Count) + sharedInventories.Sum(i => i.Items.Count)
                    };

                    ViewBag.IsAdmin = false;
                    return View("UserDashboard", userStats);
                }
            }

            // Guest/unauthenticated user - return default Index view
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
