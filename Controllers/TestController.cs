using Microsoft.AspNetCore.Mvc;
using Inventory_Management_Requirements.Services;
using Microsoft.AspNetCore.Http;
using Inventory_Management_Requirements.Data;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Models;

namespace Inventory_Management_Requirements.Controllers
{
    public class TestController : Controller
    {
        private readonly IFileStorageService _fileStorage;
        private readonly ApplicationDbContext _context;

        public TestController(IFileStorageService fileStorage, ApplicationDbContext context)
        {
            _fileStorage = fileStorage;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Please select a file to upload.";
                return View("Index");
            }

            try
            {
                Console.WriteLine($"Testing file upload: {file.FileName}, Size: {file.Length}, Type: {file.ContentType}");

                var fileUrl = await _fileStorage.UploadFileAsync(file, "test-uploads");

                Console.WriteLine($"Upload successful! URL: {fileUrl}");

                ViewBag.Message = $"File uploaded successfully! URL: {fileUrl}";
                ViewBag.FileUrl = fileUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                ViewBag.Message = $"Upload failed: {ex.Message}";
                ViewBag.Error = ex.ToString();
            }

            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> CheckDuplicates()
        {
            try
            {
                var duplicateEmails = await _context.Users
                    .GroupBy(u => u.Email)
                    .Where(g => g.Count() > 1)
                    .Select(g => new
                    {
                        Email = g.Key,
                        Count = g.Count(),
                        UserIds = g.Select(u => u.Id).ToList()
                    })
                    .ToListAsync();

                var duplicateUserNames = await _context.Users
                    .GroupBy(u => u.UserName)
                    .Where(g => g.Count() > 1)
                    .Select(g => new
                    {
                        UserName = g.Key,
                        Count = g.Count(),
                        UserIds = g.Select(u => u.Id).ToList()
                    })
                    .ToListAsync();

                ViewBag.DuplicateEmails = duplicateEmails;
                ViewBag.DuplicateUserNames = duplicateUserNames;
                ViewBag.TotalUsers = await _context.Users.CountAsync();

                return View("Duplicates");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CleanDuplicates()
        {
            try
            {
                var duplicateEmails = await _context.Users
                    .GroupBy(u => u.Email)
                    .Where(g => g.Count() > 1)
                    .ToListAsync();

                int deletedCount = 0;
                foreach (var group in duplicateEmails)
                {
                    var users = group.OrderBy(u => u.Id).ToList();
                    // Keep the first user, delete the rest
                    for (int i = 1; i < users.Count; i++)
                    {
                        _context.Users.Remove(users[i]);
                        deletedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                ViewBag.Message = $"Cleaned up {deletedCount} duplicate user records.";
                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Index");
            }
        }
    }
}
