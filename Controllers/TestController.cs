using Microsoft.AspNetCore.Mvc;
using Inventory_Management_Requirements.Services;
using Microsoft.AspNetCore.Http;

namespace Inventory_Management_Requirements.Controllers
{
    public class TestController : Controller
    {
        private readonly IFileStorageService _fileStorage;

        public TestController(IFileStorageService fileStorage)
        {
            _fileStorage = fileStorage;
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
    }
}
