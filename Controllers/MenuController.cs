using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    // Module: Menu Maintenance
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private const int PageSize = 6;

        public MenuController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Public: customer-facing menu browsing (also the customer landing page).
        // Renders the page shell; the actual item grid is loaded via AJAX from GridPartial.
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            return View();
        }

        // Module: AJAX search / filter / sort / paging - returns just the item grid HTML
        [AllowAnonymous]
        public async Task<IActionResult> GridPartial(string? search, int? categoryId, string sort = "top", int page = 1)
        {
            var query = _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Images)
                .Where(m => m.IsAvailable)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.ItemName.Contains(search) || (m.Description != null && m.Description.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(m => m.CategoryId == categoryId.Value);

            var items = await query.ToListAsync();

            // Module: Menu by Top Selling - quantity sold aggregated per menu item
            var salesByItem = await _context.OrderItems
                .GroupBy(oi => oi.MenuItemId)
                .Select(g => new { MenuItemId = g.Key, Qty = g.Sum(oi => oi.Quantity) })
                .ToDictionaryAsync(x => x.MenuItemId, x => x.Qty);

            ViewBag.SalesByItem = salesByItem;

            IEnumerable<MenuItem> sorted = sort switch
            {
                "price_asc" => items.OrderBy(m => m.Price),
                "price_desc" => items.OrderByDescending(m => m.Price),
                "name" => items.OrderBy(m => m.ItemName),
                "rating" => items.OrderByDescending(m => GetAverageRating(m.MenuItemId)),
                _ => items.OrderByDescending(m => salesByItem.GetValueOrDefault(m.MenuItemId, 0)) // "top" selling first
            };

            int totalCount = sorted.Count();
            int totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(totalPages, 1)));

            var pagedItems = sorted.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Sort = sort;

            return PartialView("_MenuGrid", pagedItems);
        }

        private double GetAverageRating(int menuItemId)
        {
            var ratings = _context.Reviews.Where(r => r.MenuItemId == menuItemId).Select(r => r.Rating).ToList();
            return ratings.Any() ? ratings.Average() : 0;
        }

        // Module: show full details (price, name, multiple photos, category) before Add to Cart
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Images)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null) return NotFound();

            var ratings = await _context.Reviews.Where(r => r.MenuItemId == id).ToListAsync();
            ViewBag.AverageRating = ratings.Any() ? ratings.Average(r => r.Rating) : (double?)null;
            ViewBag.ReviewCount = ratings.Count;

            return View(item);
        }

        // ----- Admin CRUD -----

        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> Manage()
        {
            var items = await _context.MenuItems.Include(m => m.Category).OrderBy(m => m.ItemName).ToListAsync();
            return View(items);
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> Create()
        {
            ViewBag.ExistingCategories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
            return View(new MenuItemViewModel());
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItemViewModel model)
        {
            // Manual validation for CategoryName (no longer [Required] on the view model)
            if (string.IsNullOrWhiteSpace(model.CategoryName))
                ModelState.AddModelError(nameof(model.CategoryName), "Please enter a category.");

            if (!ModelState.IsValid)
            {
                ViewBag.ExistingCategories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
                return View(model);
            }

            var category = await ResolveCategoryAsync(model.CategoryName);

            var item = new MenuItem
            {
                ItemName = model.ItemName,
                Description = model.Description,
                Price = model.Price,
                CategoryId = category.CategoryId,
                IsAvailable = model.IsAvailable,
                IsSignatureDish = model.IsSignatureDish
            };

            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();

            await SaveImagesAsync(item.MenuItemId, model.ImageFiles);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{item.ItemName}' added to menu.";
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Images)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null) return NotFound();

            ViewBag.ExistingCategories = await _context.Categories
                .OrderBy(c => c.CategoryName).ToListAsync();

            // Fallback in case the Category navigation property wasn't loaded for some reason
            string categoryName = item.Category?.CategoryName
                ?? await _context.Categories
                    .Where(c => c.CategoryId == item.CategoryId)
                    .Select(c => c.CategoryName)
                    .FirstOrDefaultAsync()
                ?? string.Empty;

            return View(new MenuItemViewModel
            {
                MenuItemId = item.MenuItemId,
                ItemName = item.ItemName,
                Description = item.Description,
                Price = item.Price,
                CategoryName = categoryName,
                IsAvailable = item.IsAvailable,
                IsSignatureDish = item.IsSignatureDish,
                ExistingImages = item.Images.Select(i => new ExistingImageViewModel
                {
                    MenuItemImageId = i.MenuItemImageId,
                    ImagePath = i.ImagePath
                }).ToList()
            });
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuItemViewModel model)
        {
            if (id != model.MenuItemId) return NotFound();

            // Manual validation for CategoryName (no longer [Required] on the view model)
            if (string.IsNullOrWhiteSpace(model.CategoryName))
                ModelState.AddModelError(nameof(model.CategoryName), "Please enter a category.");

            if (!ModelState.IsValid)
            {
                ViewBag.ExistingCategories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
                return View(model);
            }

            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            var category = await ResolveCategoryAsync(model.CategoryName);

            item.ItemName = model.ItemName;
            item.Description = model.Description;
            item.Price = model.Price;
            item.CategoryId = category.CategoryId;
            item.IsAvailable = model.IsAvailable;
            item.IsSignatureDish = model.IsSignatureDish;

            // Guard against null ImageFiles (defensive)
            if (model.ImageFiles != null && model.ImageFiles.Any(f => f.Length > 0))
                await SaveImagesAsync(item.MenuItemId, model.ImageFiles);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"'{item.ItemName}' updated.";
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int menuItemId)
        {
            var image = await _context.MenuItemImages.FindAsync(imageId);
            if (image != null)
            {
                _context.MenuItemImages.Remove(image);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = menuItemId });
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            // Menu items that have been ordered or reviewed can't be removed (Restrict FK) -
            // deleting them would also erase past order/review history, so block it instead.
            bool hasOrderHistory = await _context.OrderItems.AnyAsync(oi => oi.MenuItemId == id);
            bool hasReviews = await _context.Reviews.AnyAsync(r => r.MenuItemId == id);

            if (hasOrderHistory || hasReviews)
            {
                item.IsAvailable = false;
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = $"'{item.ItemName}' has order or review history, so it can't be deleted. It has been marked unavailable instead.";
                return RedirectToAction(nameof(Manage));
            }

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Menu item deleted.";
            return RedirectToAction(nameof(Manage));
        }

        // Module: Custom category - look up by name (case-insensitive) or create a brand new one
        private async Task<Category> ResolveCategoryAsync(string? categoryName)
        {
            string trimmed = (categoryName ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(trimmed))
                throw new InvalidOperationException("Category name is required.");

            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == trimmed.ToLower());

            if (existing != null) return existing;

            var newCategory = new Category
            {
                CategoryName = trimmed,
                DisplayOrder = await _context.Categories.CountAsync() + 1
            };
            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();
            return newCategory;
        }

        private async Task SaveImagesAsync(int menuItemId, List<IFormFile> files)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "menu");
            Directory.CreateDirectory(uploadsDir);

            int order = await _context.MenuItemImages.Where(i => i.MenuItemId == menuItemId).CountAsync();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _context.MenuItemImages.Add(new MenuItemImage
                {
                    MenuItemId = menuItemId,
                    ImagePath = $"/uploads/menu/{fileName}",
                    DisplayOrder = order++
                });
            }
        }
    }
}