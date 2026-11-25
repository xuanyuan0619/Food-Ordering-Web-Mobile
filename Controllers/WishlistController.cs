using System.Security.Claims;
using System.Text.Json;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    // Module: Wishlist (Customer favourites)
    [Authorize(Roles = nameof(UserRole.Customer))]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart"; // must match OrderController's key

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index()
        {
            var items = await _context.WishlistItems
                .Include(w => w.MenuItem).ThenInclude(m => m!.Category)
                .Where(w => w.CustomerId == CurrentUserId)
                .OrderByDescending(w => w.DateAdded)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int menuItemId, string? returnUrl = null)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null) return NotFound();

            bool alreadyExists = await _context.WishlistItems
                .AnyAsync(w => w.CustomerId == CurrentUserId && w.MenuItemId == menuItemId);

            if (!alreadyExists)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    CustomerId = CurrentUserId,
                    MenuItemId = menuItemId
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{menuItem.ItemName} added to your wishlist.";
            }
            else
            {
                TempData["SuccessMessage"] = $"{menuItem.ItemName} is already in your wishlist.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Menu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int wishlistItemId)
        {
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.WishlistItemId == wishlistItemId && w.CustomerId == CurrentUserId);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // One-click: move a wishlist item straight into the shopping cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int menuItemId, int quantity = 1)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null || !menuItem.IsAvailable)
            {
                TempData["ErrorMessage"] = "This item is currently unavailable.";
                return RedirectToAction(nameof(Index));
            }

            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            var cart = cartJson == null
                ? new List<CartItemViewModel>()
                : JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) ?? new();

            var existing = cart.FirstOrDefault(c => c.MenuItemId == menuItemId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    MenuItemId = menuItem.MenuItemId,
                    ItemName = menuItem.ItemName,
                    UnitPrice = menuItem.Price,
                    Quantity = quantity < 1 ? 1 : quantity
                });
            }

            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
            TempData["SuccessMessage"] = $"{menuItem.ItemName} added to cart.";
            return RedirectToAction(nameof(Index));
        }
    }
}
