using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;
using Inventory_Management_Requirements.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;

namespace Inventory_Management_Requirements.Controllers
{
    public class InventoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorageService _fileStorage;

        public InventoriesController(ApplicationDbContext db, IFileStorageService fileStorage)
        {
            _db = db;
            _fileStorage = fileStorage;
        }

        public IActionResult Index()
        {
            var inventories = _db.Inventories
                .Include(i => i.Category)
                .Include(i => i.CreatedBy)
                .Include(i => i.Items)
                .ToList();
            return View(inventories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var categories = _db.Categories.ToList();
            categories.Insert(0, new Category { Id = -1, Name = "Others" });
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inventory_Management_Requirements.Models.InventoryCreateViewModel viewModel, List<IFormFile> files)
        {
            Console.WriteLine("=== INVENTORY CREATE POST STARTED ===");
            Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"User name: {User.Identity.Name}");
            Console.WriteLine($"Inventory Title: {viewModel.Title}");
            Console.WriteLine($"Inventory Description: {viewModel.Description}");
            Console.WriteLine($"CategoryId: {viewModel.CategoryId}");
            Console.WriteLine($"Files count: {files?.Count ?? 0}");

            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("ERROR: User not authenticated");
                return Forbid();
            }

            // Custom validation for category
            if (viewModel.CategoryId == -1 && string.IsNullOrWhiteSpace(viewModel.CustomCategoryName))
            {
                ModelState.AddModelError("CustomCategoryName", "Custom category name is required when 'Others' is selected.");
            }
            else if (viewModel.CategoryId != -1 && viewModel.CategoryId == null)
            {
                ModelState.AddModelError("CategoryId", "Category is required.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine("ModelState is invalid:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                var categories = _db.Categories.ToList();
                categories.Insert(0, new Category { Id = -1, Name = "Others" });
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(viewModel);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"User ID: {userId}");

                int categoryId;
                if (viewModel.CategoryId == -1)
                {
                    // Create new category
                    var newCategory = new Category
                    {
                        Name = viewModel.CustomCategoryName!,
                        Description = "Custom category created by user",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Categories.Add(newCategory);
                    await _db.SaveChangesAsync();
                    categoryId = newCategory.Id;
                    Console.WriteLine($"New category created: {newCategory.Name} (ID: {categoryId})");
                }
                else
                {
                    categoryId = viewModel.CategoryId!.Value;
                }

                // Create inventory from view model
                var inventory = new Inventory
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    CategoryId = categoryId,
                    ImageUrl = viewModel.ImageUrl,
                    IsPublic = viewModel.IsPublic,
                    CreatedById = userId ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine("Adding inventory to database...");

                _db.Inventories.Add(inventory);
                var saveResult = await _db.SaveChangesAsync();
                Console.WriteLine($"Inventory saved. Rows affected: {saveResult}");
                Console.WriteLine($"New inventory ID: {inventory.Id}");

                // Handle file uploads
                if (files != null && files.Count > 0)
                {
                    Console.WriteLine($"Processing {files.Count} files...");
                    foreach (var file in files)
                    {
                        Console.WriteLine($"Processing file: {file.FileName}, Size: {file.Length}, ContentType: {file.ContentType}");
                        if (file.Length > 0)
                        {
                            try
                            {
                                var fileUrl = await _fileStorage.UploadFileAsync(file, "inventory-attachments");
                                Console.WriteLine($"File uploaded successfully. URL: {fileUrl}");

                                var fileType = GetFileType(file.ContentType);
                                Console.WriteLine($"File type determined: {fileType}");

                                var attachment = new InventoryAttachment
                                {
                                    InventoryId = inventory.Id,
                                    FileName = file.FileName,
                                    FileUrl = fileUrl,
                                    FileType = fileType,
                                    FileSize = file.Length,
                                    UploadedById = userId
                                };

                                _db.InventoryAttachments.Add(attachment);
                                Console.WriteLine("Attachment added to database");
                            }
                            catch (Exception fileEx)
                            {
                                Console.WriteLine($"ERROR uploading file {file.FileName}: {fileEx.Message}");
                                // Continue with other files
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Skipping empty file: {file.FileName}");
                        }
                    }
                    var attachmentSaveResult = await _db.SaveChangesAsync();
                    Console.WriteLine($"Attachments saved. Rows affected: {attachmentSaveResult}");
                }
                else
                {
                    Console.WriteLine("No files to process");
                }

                // Create default ID format
                Console.WriteLine("Creating default ID format...");
                _db.CustomIdFormats.Add(new CustomIdFormat
                {
                    InventoryId = inventory.Id,
                    FormatDefinition = JsonSerializer.Serialize(new object[] {
                        new { type = "fixed", value = "ITEM-" },
                        new { type = "seq", pad = 4 }
                    })
                });
                var idFormatSaveResult = await _db.SaveChangesAsync();
                Console.WriteLine($"ID format saved. Rows affected: {idFormatSaveResult}");

                Console.WriteLine($"=== INVENTORY CREATE COMPLETED SUCCESSFULLY ===");
                Console.WriteLine($"Redirecting to Details page for inventory ID: {inventory.Id}");

                return RedirectToAction("Details", new { id = inventory.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION IN CREATE POST ===");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Exception message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                ViewBag.Categories = new SelectList(_db.Categories, "Id", "Name");
                ModelState.AddModelError("", $"An error occurred while creating the inventory: {ex.Message}");
                return View(viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(Inventory inventory)
        {
            Console.WriteLine("=== UPDATE SETTINGS POST STARTED ===");
            Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"Inventory ID: {inventory.Id}");
            Console.WriteLine($"Title: {inventory.Title}");
            Console.WriteLine($"Description: {inventory.Description}");
            Console.WriteLine($"CategoryId: {inventory.CategoryId}");
            Console.WriteLine($"IsPublic: {inventory.IsPublic}");

            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("ERROR: User not authenticated");
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine("ModelState is invalid:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return RedirectToAction("Settings", new { id = inventory.Id });
            }

            try
            {
                var existingInventory = await _db.Inventories.FindAsync(inventory.Id);
                if (existingInventory == null)
                {
                    Console.WriteLine("ERROR: Inventory not found");
                    return NotFound();
                }

                Console.WriteLine($"Existing inventory - Title: {existingInventory.Title}, IsPublic: {existingInventory.IsPublic}");

                existingInventory.Title = inventory.Title;
                existingInventory.Description = inventory.Description;
                existingInventory.CategoryId = inventory.CategoryId;
                existingInventory.IsPublic = inventory.IsPublic;

                Console.WriteLine("Updating inventory in database...");
                _db.Inventories.Update(existingInventory);
                var saveResult = await _db.SaveChangesAsync();
                Console.WriteLine($"Database update completed. Rows affected: {saveResult}");

                Console.WriteLine($"=== UPDATE SETTINGS COMPLETED SUCCESSFULLY ===");
                return RedirectToAction("Details", new { id = existingInventory.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION IN UPDATE SETTINGS ===");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Exception message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"An error occurred while updating the inventory settings: {ex.Message}";
                return RedirectToAction("Settings", new { id = inventory.Id });
            }
        }

        private string GetFileType(string contentType)
        {
            if (contentType.StartsWith("image/")) return "image";
            if (contentType == "application/pdf") return "pdf";
            if (contentType.Contains("document") || contentType.Contains("word")) return "document";
            return "other";
        }

        public IActionResult Details(int id)
        {
            var inventory = _db.Inventories
                .Include(i => i.Fields)
                .Include(i => i.Items)
                .ThenInclude(i => i.CreatedBy)
                .Include(i => i.Category)
                .Include(i => i.IdFormat)
                .Include(i => i.Attachments)
                .ThenInclude(a => a.UploadedBy)
                .FirstOrDefault(i => i.Id == id);

            if (inventory == null) return NotFound();

            var comments = _db.Comments
                .Include(c => c.User)
                .Where(c => c.InventoryId == id && c.AttachmentId == null) // Only inventory-wide comments
                .ToList();

            var accessList = _db.InventoryAccesses
                .Include(a => a.User)
                .Where(a => a.InventoryId == id)
                .ToList();

            var attachments = _db.InventoryAttachments
                .Include(a => a.UploadedBy)
                .Where(a => a.InventoryId == id)
                .ToList();

            // Load attachment-specific comments
            foreach (var attachment in attachments)
            {
                attachment.Comments = _db.Comments
                    .Include(c => c.User)
                    .Where(c => c.AttachmentId == attachment.Id)
                    .ToList();
            }

            var users = _db.Users.ToList();

            ViewData["Categories"] = _db.Categories.ToList();
            ViewData["Users"] = users;
            ViewData["InventoryId"] = id;

            var viewModel = new InventoryDetailsViewModel
            {
                Inventory = inventory,
                Comments = comments,
                AccessList = accessList,
                Attachments = attachments
            };

            return View("Details", viewModel);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var inventory = _db.Inventories.Find(id);
            if (inventory == null) return NotFound();

            ViewBag.Categories = new SelectList(_db.Categories, "Id", "Name");
            return View(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Inventory inventory, IFormFile image)
        {
            if (!User.Identity.IsAuthenticated) return Forbid();

            if (ModelState.IsValid)
            {
                if (image != null)
                    inventory.ImageUrl = await _fileStorage.UploadFileAsync(image, "inventory-images");

                _db.Inventories.Update(inventory);
                await _db.SaveChangesAsync();
                return RedirectToAction("Details", new { id = inventory.Id });
            }

            ViewBag.Categories = new SelectList(_db.Categories, "Id", "Name");
            return View(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var inventory = await _db.Inventories.FindAsync(id);
            if (inventory == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && inventory.CreatedById != userId)
            {
                return Forbid();
            }

            _db.Inventories.Remove(inventory);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventories/GetTagSuggestions
        public async Task<IActionResult> GetTagSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<string>());
            }

            var tags = await _db.Tags
                .Where(t => t.Name.ToLower().Contains(term.ToLower()))
                .OrderByDescending(t => t.UsageCount)
                .Take(10)
                .Select(t => t.Name)
                .ToListAsync();

            return Json(tags);
        }

        // POST: Inventories/AddUserAccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserAccess(int inventoryId, string userId)
        {
            var inventoryAccess = new InventoryAccess
            {
                InventoryId = inventoryId,
                UserId = userId,
                GrantedById = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _db.InventoryAccesses.Add(inventoryAccess);
            await _db.SaveChangesAsync();
            return RedirectToAction("Details", new { id = inventoryId });
        }

        // POST: Inventories/RemoveUserAccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserAccess(int accessId)
        {
            var access = await _db.InventoryAccesses.FindAsync(accessId);
            if (access == null) return NotFound();

            _db.InventoryAccesses.Remove(access);
            await _db.SaveChangesAsync();
            return RedirectToAction("Details", new { id = access.InventoryId });
        }

        [HttpGet]
        public IActionResult Settings(int id)
        {
            var inventory = _db.Inventories
                .Include(i => i.Category)
                .Include(i => i.CreatedBy)
                .FirstOrDefault(i => i.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            ViewData["Categories"] = _db.Categories.ToList();
            return View(inventory);
        }

        [HttpGet]
        public IActionResult AttachmentSettings(int attachmentId)
        {
            var attachment = _db.InventoryAttachments
                .Include(a => a.Inventory)
                .Include(a => a.UploadedBy)
                .FirstOrDefault(a => a.Id == attachmentId);

            if (attachment == null)
            {
                return NotFound();
            }

            // Assuming the Settings page uses the same model as the _Settings partial but adapted for attachment
            // You may need to create a new ViewModel if necessary
            return View("Settings", attachment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttachmentSettings(InventoryAttachment attachment)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                // Reload the attachment with related data for the view
                var existingAttachment = await _db.InventoryAttachments
                    .Include(a => a.Inventory)
                    .Include(a => a.UploadedBy)
                    .FirstOrDefaultAsync(a => a.Id == attachment.Id);

                if (existingAttachment == null)
                {
                    return NotFound();
                }

                return View("Settings", existingAttachment);
            }

            try
            {
                var existingAttachment = await _db.InventoryAttachments.FindAsync(attachment.Id);
                if (existingAttachment == null)
                {
                    return NotFound();
                }

                // Only update the filename (other properties are read-only)
                existingAttachment.FileName = attachment.FileName;

                _db.InventoryAttachments.Update(existingAttachment);
                await _db.SaveChangesAsync();

                return RedirectToAction("Settings", new { attachmentId = existingAttachment.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the attachment settings: {ex.Message}");

                // Reload the attachment with related data for the view
                var existingAttachment = await _db.InventoryAttachments
                    .Include(a => a.Inventory)
                    .Include(a => a.UploadedBy)
                    .FirstOrDefaultAsync(a => a.Id == attachment.Id);

                if (existingAttachment == null)
                {
                    return NotFound();
                }

                return View("Settings", existingAttachment);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int inventoryId, string content, int? attachmentId = null)
        {
            Console.WriteLine("=== ADD COMMENT STARTED ===");
            Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"Inventory ID: {inventoryId}");
            Console.WriteLine($"Attachment ID: {attachmentId}");
            Console.WriteLine($"Content length: {content?.Length ?? 0}");

            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("ERROR: User not authenticated");
                return Json(new { success = false, message = "User not authenticated" });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                Console.WriteLine("ERROR: Comment content is empty");
                return Json(new { success = false, message = "Comment content cannot be empty" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"User ID: {userId}");

                var comment = new Comment
                {
                    InventoryId = inventoryId,
                    AttachmentId = attachmentId,
                    UserId = userId,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine("Adding comment to database...");
                _db.Comments.Add(comment);
                var saveResult = await _db.SaveChangesAsync();
                Console.WriteLine($"Database save completed. Rows affected: {saveResult}");
                Console.WriteLine($"Comment ID: {comment.Id}");

                Console.WriteLine("=== ADD COMMENT COMPLETED SUCCESSFULLY ===");
                return Json(new { success = true, message = "Comment added successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION IN ADD COMMENT ===");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Exception message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return Json(new { success = false, message = $"Error adding comment: {ex.Message}" });
            }
        }
    }
}
