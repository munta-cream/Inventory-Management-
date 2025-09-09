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
using System.Net.Http;

namespace Inventory_Management_Requirements.Controllers
{
    public class InventoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorageService _fileStorage;
        private readonly ICustomIdGenerator _idGenerator;

        public InventoriesController(ApplicationDbContext db, IFileStorageService fileStorage, ICustomIdGenerator idGenerator)
        {
            _db = db;
            _fileStorage = fileStorage;
            _idGenerator = idGenerator;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var inventories = await _db.Inventories
                .Include(i => i.Category)
                .Include(i => i.CreatedBy)
                .Include(i => i.Items)
                .Where(i => i.CreatedById == userId || i.IsPublic || isAdmin ||
                           _db.InventoryAccesses.Any(a => a.InventoryId == i.Id && a.UserId == userId))
                .ToListAsync();

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
            Console.WriteLine($"Item Name: {viewModel.ItemName}");
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

                // Create item if item data is provided
                if (!string.IsNullOrWhiteSpace(viewModel.ItemName))
                {
                    Console.WriteLine("Creating item for the inventory...");

                    // Generate CustomId for the item
                    var nextSequence = (_db.Items.Where(i => i.InventoryId == inventory.Id).Max(i => (int?)i.Id) ?? 0) + 1;
                    var idFormat = _db.CustomIdFormats.FirstOrDefault(f => f.InventoryId == inventory.Id);

                    string customId = string.Empty;
                    if (idFormat != null)
                    {
                        try
                        {
                            using var formatDoc = JsonDocument.Parse(idFormat.FormatDefinition);
                            customId = _idGenerator.Generate(inventory.Id, formatDoc, nextSequence);
                            Console.WriteLine($"Generated CustomId: {customId}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR generating CustomId: {ex.Message}");
                            // Fallback to default format
                            customId = $"ITEM-{nextSequence:D4}";
                        }
                    }
                    else
                    {
                        // Fallback if no format is defined
                        customId = $"ITEM-{nextSequence:D4}";
                        Console.WriteLine($"No ID format found, using fallback: {customId}");
                    }

                    var item = new Item
                    {
                        InventoryId = inventory.Id,
                        CustomId = customId,
                        Name = viewModel.ItemName,
                        Description = viewModel.ItemDescription,
                        Brand = viewModel.Brand,
                        Model = viewModel.Model,
                        Quantity = viewModel.Quantity ?? 0,
                        Price = viewModel.Price ?? 0,
                        Weight = viewModel.Weight ?? 0,
                        InStock = viewModel.InStock,
                        Fragile = viewModel.Fragile,
                        Perishable = viewModel.Perishable,
                        CreatedById = userId ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.Items.Add(item);
                    var itemSaveResult = await _db.SaveChangesAsync();
                    Console.WriteLine($"Item saved. Rows affected: {itemSaveResult}");
                    Console.WriteLine($"New item ID: {item.Id}, CustomId: {item.CustomId}");
                }
                else
                {
                    Console.WriteLine("No item data provided, skipping item creation");
                }

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

                TempData["SuccessMessage"] = "Inventory created successfully!";
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
                TempData["ErrorMessage"] = $"An error occurred while creating the inventory: {ex.Message}";
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

            // Check access control
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var hasAccess = await HasAccessToInventory(inventory.Id, userId, isAdmin);
            if (!hasAccess)
            {
                Console.WriteLine("ERROR: User does not have access to this inventory");
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

        private async Task<bool> HasAccessToInventory(int inventoryId, string userId, bool isAdmin)
        {
            var inventory = await _db.Inventories.FindAsync(inventoryId);
            if (inventory == null) return false;

            // User has access if:
            // 1. They created the inventory
            // 2. The inventory is public
            // 3. They are an admin
            // 4. They have been granted access
            return inventory.CreatedById == userId ||
                   inventory.IsPublic ||
                   isAdmin ||
                   await _db.InventoryAccesses.AnyAsync(a => a.InventoryId == inventoryId && a.UserId == userId);
        }

        private string GetFileType(string contentType)
        {
            if (contentType.StartsWith("image/")) return "image";
            if (contentType == "application/pdf") return "pdf";
            if (contentType.Contains("document") || contentType.Contains("word")) return "document";
            return "other";
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var inventory = await _db.Inventories
                .Include(i => i.Fields)
                .Include(i => i.Items)
                .ThenInclude(i => i.CreatedBy)
                .Include(i => i.Category)
                .Include(i => i.IdFormat)
                .Include(i => i.Attachments)
                .ThenInclude(a => a.UploadedBy)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null) return NotFound();

            var hasAccess = inventory.CreatedById == userId ||
                            inventory.IsPublic ||
                            isAdmin ||
                            await _db.InventoryAccesses.AnyAsync(a => a.InventoryId == id && a.UserId == userId);

            if (!hasAccess)
            {
                return Forbid();
            }

            var comments = await _db.Comments
                .Include(c => c.User)
                .Where(c => c.InventoryId == id && c.AttachmentId == null) // Only inventory-wide comments
                .ToListAsync();

            var accessList = await _db.InventoryAccesses
                .Include(a => a.User)
                .Where(a => a.InventoryId == id)
                .ToListAsync();

            var attachments = await _db.InventoryAttachments
                .Include(a => a.UploadedBy)
                .Where(a => a.InventoryId == id)
                .ToListAsync();

            // Load attachment-specific comments
            foreach (var attachment in attachments)
            {
                attachment.Comments = await _db.Comments
                    .Include(c => c.User)
                    .Where(c => c.AttachmentId == attachment.Id)
                    .ToListAsync();
            }

            var users = await _db.Users.ToListAsync();

            ViewData["Categories"] = await _db.Categories.ToListAsync();
            ViewData["Users"] = users;
            ViewData["InventoryId"] = id;

            var items = await _db.Items
                .Include(i => i.CreatedBy)
                .Where(i => i.InventoryId == id)
                .ToListAsync();

            var viewModel = new InventoryDetailsViewModel
            {
                Inventory = inventory,
                Comments = comments,
                AccessList = accessList,
                Attachments = attachments,
                Items = items
            };

            return View("Details", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var inventory = _db.Inventories.Find(id);
            if (inventory == null) return NotFound();

            // Check access control
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var hasAccess = await HasAccessToInventory(id, userId, isAdmin);
            if (!hasAccess) return Forbid();

            ViewBag.Categories = new SelectList(_db.Categories, "Id", "Name");
            return View(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Inventory inventory, IFormFile image)
        {
            if (!User.Identity.IsAuthenticated) return Forbid();

            // Check access control
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var hasAccess = await HasAccessToInventory(inventory.Id, userId, isAdmin);
            if (!hasAccess) return Forbid();

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
            // Check access control - only inventory owner or admin can grant access
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var inventory = await _db.Inventories.FindAsync(inventoryId);

            if (inventory == null) return NotFound();

            if (!isAdmin && inventory.CreatedById != currentUserId)
            {
                return Forbid();
            }

            var inventoryAccess = new InventoryAccess
            {
                InventoryId = inventoryId,
                UserId = userId,
                GrantedById = currentUserId
            };

            _db.InventoryAccesses.Add(inventoryAccess);
            await _db.SaveChangesAsync();
            return RedirectToAction("Details", new { id = inventoryId });
        }

        // POST: Inventories/RemoveUserAccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserAccess(int inventoryId, string userId)
        {
            // Check access control - only inventory owner or admin can remove access
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var inventory = await _db.Inventories.FindAsync(inventoryId);

            if (inventory == null) return NotFound();

            if (!isAdmin && inventory.CreatedById != currentUserId)
            {
                return Forbid();
            }

            var access = await _db.InventoryAccesses
                .FirstOrDefaultAsync(a => a.InventoryId == inventoryId && a.UserId == userId);
            if (access == null) return NotFound();

            _db.InventoryAccesses.Remove(access);
            await _db.SaveChangesAsync();
            return RedirectToAction("Details", new { id = inventoryId });
        }

        [HttpGet]
        public async Task<IActionResult> Settings(int id)
        {
            var inventory = _db.Inventories
                .Include(i => i.Category)
                .Include(i => i.CreatedBy)
                .FirstOrDefault(i => i.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            // Check access control
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var hasAccess = await HasAccessToInventory(id, userId, isAdmin);
            if (!hasAccess) return Forbid();

            ViewData["Categories"] = _db.Categories.ToList();
            return View(inventory);
        }

        [HttpGet]
        public async Task<IActionResult> AttachmentSettings(int attachmentId)
        {
            var attachment = _db.InventoryAttachments
                .Include(a => a.Inventory)
                .Include(a => a.UploadedBy)
                .FirstOrDefault(a => a.Id == attachmentId);

            if (attachment == null)
            {
                return NotFound();
            }

            // Check access control - user must have access to the inventory
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var hasAccess = await HasAccessToInventory(attachment.InventoryId, userId, isAdmin);
            if (!hasAccess) return Forbid();

            // Create a view model for attachment settings
            var viewModel = new
            {
                Attachment = attachment,
                Inventory = attachment.Inventory
            };

            return View("AttachmentSettings", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttachmentSettings(int attachmentId, string fileName, string description)
        {
            Console.WriteLine("=== UPDATE ATTACHMENT SETTINGS STARTED ===");
            Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"Attachment ID: {attachmentId}");
            Console.WriteLine($"File Name: {fileName}");

            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("ERROR: User not authenticated");
                return Forbid();
            }

            try
            {
                var attachment = await _db.InventoryAttachments
                    .Include(a => a.Inventory)
                    .FirstOrDefaultAsync(a => a.Id == attachmentId);

                if (attachment == null)
                {
                    Console.WriteLine("ERROR: Attachment not found");
                    return NotFound();
                }

                // Check access control - user must have access to the inventory
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                var hasAccess = await HasAccessToInventory(attachment.InventoryId, userId, isAdmin);
                if (!hasAccess)
                {
                    Console.WriteLine("ERROR: User does not have access to this inventory");
                    return Forbid();
                }

                Console.WriteLine($"Updating attachment: {attachment.FileName}");

                attachment.FileName = fileName;
                // Note: Description field may need to be added to InventoryAttachment model if not present

                _db.InventoryAttachments.Update(attachment);
                var saveResult = await _db.SaveChangesAsync();
                Console.WriteLine($"Database update completed. Rows affected: {saveResult}");

                Console.WriteLine("=== UPDATE ATTACHMENT SETTINGS COMPLETED SUCCESSFULLY ===");
                TempData["SuccessMessage"] = "Attachment settings updated successfully!";
                return RedirectToAction("Details", new { id = attachment.InventoryId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION IN UPDATE ATTACHMENT SETTINGS ===");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Exception message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"An error occurred while updating attachment settings: {ex.Message}";
                return RedirectToAction("AttachmentSettings", new { attachmentId = attachmentId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItemDetails(int inventoryId, int itemId, string name, string description, string brand, string model, int quantity, decimal price, decimal weight, bool inStock, bool fragile, bool perishable)
        {
            Console.WriteLine("=== UPDATE ITEM DETAILS STARTED ===");
            Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"Inventory ID: {inventoryId}");
            Console.WriteLine($"Item ID: {itemId}");

            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("ERROR: User not authenticated");
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Check access control
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var hasAccess = await HasAccessToInventory(inventoryId, userId, isAdmin);
            if (!hasAccess)
            {
                Console.WriteLine("ERROR: User does not have access to this inventory");
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                var existingItem = await _db.Items.FindAsync(itemId);
                if (existingItem == null)
                {
                    Console.WriteLine("ERROR: Item not found");
                    return Json(new { success = false, message = "Item not found" });
                }

                if (existingItem.InventoryId != inventoryId)
                {
                    Console.WriteLine("ERROR: Item does not belong to this inventory");
                    return Json(new { success = false, message = "Item does not belong to this inventory" });
                }

                Console.WriteLine($"Updating item: {existingItem.Name}");

                existingItem.Name = name;
                existingItem.Description = description;
                existingItem.Brand = brand;
                existingItem.Model = model;
                existingItem.Quantity = quantity;
                existingItem.Price = price;
                existingItem.Weight = weight;
                existingItem.InStock = inStock;
                existingItem.Fragile = fragile;
                existingItem.Perishable = perishable;
                existingItem.UpdatedAt = DateTime.UtcNow;

                _db.Items.Update(existingItem);
                var saveResult = await _db.SaveChangesAsync();
                Console.WriteLine($"Database update completed. Rows affected: {saveResult}");

                Console.WriteLine("=== UPDATE ITEM DETAILS COMPLETED SUCCESSFULLY ===");
                return Json(new { success = true, message = "Item details updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION IN UPDATE ITEM DETAILS ===");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Exception message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return Json(new { success = false, message = $"Error updating item details: {ex.Message}" });
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

            // Check access control
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var hasAccess = await HasAccessToInventory(inventoryId, userId, isAdmin);
            if (!hasAccess)
            {
                Console.WriteLine("ERROR: User does not have access to this inventory");
                return Json(new { success = false, message = "Access denied" });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                Console.WriteLine("ERROR: Comment content is empty");
                return Json(new { success = false, message = "Comment content cannot be empty" });
            }

            try
            {
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

        [HttpGet]
        public async Task<IActionResult> DownloadInventoryCsv(int id)
        {
            // Check access control
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var hasAccess = await HasAccessToInventory(id, userId, isAdmin);
            if (!hasAccess) return Forbid();

            var inventory = await _db.Inventories
                .Include(i => i.Category)
                .Include(i => i.CreatedBy)
                .Include(i => i.Items)
                .Include(i => i.Attachments)
                .ThenInclude(a => a.UploadedBy)
                .Include(i => i.AccessList)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            var comments = await _db.Comments
                .Include(c => c.User)
                .Where(c => c.InventoryId == id)
                .ToListAsync();

            var csvContent = GenerateInventoryCsv(inventory, comments);

            var fileName = $"Inventory_{inventory.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);

            return File(fileBytes, "text/csv", fileName);
        }

        private string GenerateInventoryCsv(Inventory inventory, List<Comment> comments)
        {
            var csv = new System.Text.StringBuilder();

            // Inventory Details Section
            csv.AppendLine("INVENTORY DETAILS");
            csv.AppendLine("Field,Value");
            csv.AppendLine($"Title,\"{inventory.Title}\"");
            csv.AppendLine($"Description,\"{inventory.Description?.Replace("\"", "\"\"")}\"");
            csv.AppendLine($"Category,\"{inventory.Category?.Name}\"");
            csv.AppendLine($"Created By,\"{inventory.CreatedBy?.UserName}\"");
            csv.AppendLine($"Created At,\"{inventory.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            csv.AppendLine($"Is Public,\"{inventory.IsPublic}\"");
            csv.AppendLine();

            // Items Section
            csv.AppendLine("ITEMS");
            csv.AppendLine("CustomId,Name,Description,Brand,Model,Quantity,Price,Weight,InStock,Fragile,Perishable,CreatedBy,CreatedAt");
            foreach (var item in inventory.Items)
            {
                csv.AppendLine($"\"{item.CustomId}\",\"{item.Name}\",\"{item.Description?.Replace("\"", "\"\"")}\",\"{item.Brand}\",\"{item.Model}\",\"{item.Quantity}\",\"{item.Price}\",\"{item.Weight}\",\"{item.InStock}\",\"{item.Fragile}\",\"{item.Perishable}\",\"{item.CreatedBy?.UserName}\",\"{item.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            csv.AppendLine();

            // Attachments Section
            csv.AppendLine("ATTACHMENTS");
            csv.AppendLine("FileName,FileType,FileSize,UploadedBy,UploadedAt");
            foreach (var attachment in inventory.Attachments)
            {
                csv.AppendLine($"\"{attachment.FileName}\",\"{attachment.FileType}\",\"{attachment.FileSize}\",\"{attachment.UploadedBy?.UserName}\",\"{attachment.UploadedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            csv.AppendLine();

            // Access List Section
            csv.AppendLine("ACCESS LIST");
            csv.AppendLine("UserName,GrantedBy,GrantedAt");
            foreach (var access in inventory.AccessList)
            {
                csv.AppendLine($"\"{access.User?.UserName}\",\"{access.GrantedBy?.UserName}\",\"{access.GrantedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            csv.AppendLine();

            // Comments Section
            csv.AppendLine("COMMENTS");
            csv.AppendLine("UserName,Content,CreatedAt");
            foreach (var comment in comments)
            {
                csv.AppendLine($"\"{comment.User?.UserName}\",\"{comment.Content?.Replace("\"", "\"\"")}\",\"{comment.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            return csv.ToString();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            Console.WriteLine($"=== DOWNLOAD ATTACHMENT STARTED ===");
            Console.WriteLine($"Attachment ID: {id}");
            Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"User name: {User.Identity.Name}");

            var attachment = await _db.InventoryAttachments
                .Include(a => a.Inventory)
                .FirstOrDefaultAsync(a => a.Id == id);

            Console.WriteLine($"Attachment found: {attachment != null}");
            if (attachment != null)
            {
                Console.WriteLine($"Attachment details - FileName: {attachment.FileName}, FileUrl: {attachment.FileUrl}");
                Console.WriteLine($"Inventory ID: {attachment.InventoryId}, Inventory Title: {attachment.Inventory?.Title}");
                Console.WriteLine($"Inventory CreatedById: {attachment.Inventory?.CreatedById}");
                Console.WriteLine($"Inventory IsPublic: {attachment.Inventory?.IsPublic}");
            }

            if (attachment == null)
            {
                Console.WriteLine("ERROR: Attachment not found");
                return NotFound();
            }

            // Check if user has access to the inventory
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"Current User ID: {userId}");
            Console.WriteLine($"User is Admin: {User.IsInRole("Admin")}");

            var hasAccess = attachment.Inventory.CreatedById == userId ||
                           attachment.Inventory.IsPublic ||
                           User.IsInRole("Admin") ||
                           await _db.InventoryAccesses.AnyAsync(a => a.InventoryId == attachment.InventoryId && a.UserId == userId);

            Console.WriteLine($"Access check - CreatedBy match: {attachment.Inventory.CreatedById == userId}");
            Console.WriteLine($"Access check - IsPublic: {attachment.Inventory.IsPublic}");
            Console.WriteLine($"Access check - IsAdmin: {User.IsInRole("Admin")}");
            Console.WriteLine($"Access check - Has granted access: {await _db.InventoryAccesses.AnyAsync(a => a.InventoryId == attachment.InventoryId && a.UserId == userId)}");
            Console.WriteLine($"Final access result: {hasAccess}");

            if (!hasAccess)
            {
                Console.WriteLine("ERROR: User does not have access to this attachment");
                return Forbid();
            }

            try
            {
                Console.WriteLine($"Attempting to download from URL: {attachment.FileUrl}");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(attachment.FileUrl);

                Console.WriteLine($"Cloudinary response status: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ERROR: Failed to download from Cloudinary - Status: {response.StatusCode}");
                    return NotFound();
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                Console.WriteLine($"File downloaded successfully - Size: {fileBytes.Length} bytes, ContentType: {contentType}");

                // Set Content-Disposition to force download
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{attachment.FileName}\"");

                Console.WriteLine($"=== DOWNLOAD ATTACHMENT COMPLETED SUCCESSFULLY ===");
                return File(fileBytes, contentType, attachment.FileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION IN DOWNLOAD ATTACHMENT ===");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Exception message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, "Error downloading file");
            }
        }
    }
}
