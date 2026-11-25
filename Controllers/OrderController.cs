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
    // Module: Ordering & Cart / My Order
    [Authorize(Roles = nameof(UserRole.Customer))]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";

        // Points economy constants (Module: Membership & Points)
        private const int PointsEarnedPerRM = 1;         // 1 point earned per RM1 spent
        private const decimal RmValuePerPoint = 0.05m;   // 100 points = RM5 discount when redeemed

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private List<CartItemViewModel> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            return json == null
                ? new List<CartItemViewModel>()
                : JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? new();
        }

        private void SaveCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        public IActionResult Cart()
        {
            return View(GetCart());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int menuItemId, int quantity = 1, string? specialInstructions = null)
        {
            if (quantity < 1) quantity = 1;

            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null || !menuItem.IsAvailable) return NotFound();

            var cart = GetCart();
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
                    Quantity = quantity,
                    SpecialInstructions = specialInstructions
                });
            }

            SaveCart(cart);
            TempData["SuccessMessage"] = $"{menuItem.ItemName} added to cart.";
            return RedirectToAction("Index", "Menu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int menuItemId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.MenuItemId == menuItemId);
            if (item != null)
            {
                if (quantity <= 0) cart.Remove(item);
                else item.Quantity = quantity;
            }
            SaveCart(cart);
            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int menuItemId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.MenuItemId == menuItemId);
            SaveCart(cart);
            return RedirectToAction(nameof(Cart));
        }

        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction(nameof(Cart));
            }

            var user = await _context.Users.FindAsync(CurrentUserId);

            ViewBag.Tables = await _context.DiningTables
                .Where(t => t.Status == TableStatus.Available)
                .ToListAsync();

            return View(new CheckoutViewModel { Items = cart, AvailablePoints = user?.PointsBalance ?? 0 });
        }

        // Validates a voucher code against the given subtotal. Returns the discount amount, or 0 with an error message.
        private async Task<(decimal discount, string? error)> ValidateVoucherAsync(string? code, decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return (0, null);

            var voucher = await _context.VoucherCodes
                .FirstOrDefaultAsync(v => v.Code == code.Trim().ToUpper() && v.IsActive);

            if (voucher == null) return (0, "Voucher code is invalid or no longer active.");
            if (voucher.ExpiryDate < DateTime.Now) return (0, "This voucher has expired.");
            if (subtotal < voucher.MinSpend) return (0, $"This voucher requires a minimum spend of RM{voucher.MinSpend:0.00}.");

            decimal discount = voucher.DiscountType == DiscountType.Percentage
                ? Math.Round(subtotal * (voucher.DiscountValue / 100m), 2)
                : voucher.DiscountValue;

            return (Math.Min(discount, subtotal), null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            model.Items = cart;

            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction(nameof(Cart));
            }

            var user = await _context.Users.FindAsync(CurrentUserId);
            model.AvailablePoints = user?.PointsBalance ?? 0;

            if (!ModelState.IsValid)
            {
                ViewBag.Tables = await _context.DiningTables.Where(t => t.Status == TableStatus.Available).ToListAsync();
                return View(model);
            }

            decimal subtotal = cart.Sum(i => i.Subtotal);

            // --- Discount & Voucher module ---
            var (voucherDiscount, voucherError) = await ValidateVoucherAsync(model.VoucherCode, subtotal);
            if (voucherError != null)
            {
                ModelState.AddModelError(nameof(model.VoucherCode), voucherError);
                ViewBag.Tables = await _context.DiningTables.Where(t => t.Status == TableStatus.Available).ToListAsync();
                return View(model);
            }

            // --- Membership & Points module: redeem points for a cash discount ---
            int pointsToRedeem = 0;
            decimal pointsDiscount = 0;
            if (model.UsePoints && model.PointsToRedeem > 0)
            {
                if (user == null || model.PointsToRedeem > user.PointsBalance)
                {
                    ModelState.AddModelError(nameof(model.PointsToRedeem), "You don't have enough points.");
                    ViewBag.Tables = await _context.DiningTables.Where(t => t.Status == TableStatus.Available).ToListAsync();
                    return View(model);
                }

                pointsToRedeem = model.PointsToRedeem;
                pointsDiscount = Math.Round(pointsToRedeem * RmValuePerPoint, 2);
            }

            decimal totalDiscount = Math.Min(voucherDiscount + pointsDiscount, subtotal);
            decimal finalTotal = subtotal - totalDiscount;

            var order = new Order
            {
                CustomerId = CurrentUserId,
                TableId = model.TableId,
                Status = OrderStatus.Pending,
                SubtotalAmount = subtotal,
                DiscountAmount = totalDiscount,
                VoucherCodeUsed = voucherDiscount > 0 ? model.VoucherCode!.Trim().ToUpper() : null,
                PointsRedeemed = pointsToRedeem,
                TotalAmount = finalTotal,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = model.PaymentMethod == PaymentMethod.Cash ? PaymentStatus.Pending : PaymentStatus.Paid,
                Remarks = model.Remarks,
                OrderItems = cart.Select(i => new OrderItem
                {
                    MenuItemId = i.MenuItemId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    SpecialInstructions = i.SpecialInstructions
                }).ToList()
            };

            _context.Orders.Add(order);

            // Deduct redeemed points immediately
            if (user != null && pointsToRedeem > 0)
            {
                user.PointsBalance -= pointsToRedeem;
            }

            var table = await _context.DiningTables.FindAsync(model.TableId);
            if (table != null) table.Status = TableStatus.Occupied;

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(CartSessionKey);

            return RedirectToAction(nameof(Confirmation), new { id = order.OrderId });
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == CurrentUserId);

            if (order == null) return NotFound();
            return View(order);
        }

        // Module: Order History
        public async Task<IActionResult> History()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.CustomerId == CurrentUserId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // Module: Order History detail - full breakdown + review form for completed dishes
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == CurrentUserId);

            if (order == null) return NotFound();

            var reviewedOrderItemIds = await _context.Reviews
                .Where(r => r.OrderItem!.OrderId == id)
                .Select(r => r.OrderItemId)
                .ToListAsync();

            ViewBag.ReviewedOrderItemIds = reviewedOrderItemIds;
            return View(order);
        }

        // Module: Reorder - copy a past order's items back into the cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == CurrentUserId);

            if (order == null) return NotFound();

            var cart = GetCart();
            int addedCount = 0;

            foreach (var oi in order.OrderItems)
            {
                if (oi.MenuItem == null || !oi.MenuItem.IsAvailable) continue;

                var existing = cart.FirstOrDefault(c => c.MenuItemId == oi.MenuItemId);
                if (existing != null)
                {
                    existing.Quantity += oi.Quantity;
                }
                else
                {
                    cart.Add(new CartItemViewModel
                    {
                        MenuItemId = oi.MenuItemId,
                        ItemName = oi.MenuItem.ItemName,
                        UnitPrice = oi.MenuItem.Price,
                        Quantity = oi.Quantity
                    });
                }
                addedCount++;
            }

            SaveCart(cart);

            TempData["SuccessMessage"] = addedCount > 0
                ? "Items from this order have been added to your cart."
                : "None of the items from this order are available anymore.";

            return RedirectToAction(nameof(Cart));
        }

        // Module: Review (five-star rating on a completed dish)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int orderId, ReviewViewModel model)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == model.OrderItemId && oi.Order!.CustomerId == CurrentUserId);

            if (orderItem == null || orderItem.Order!.Status != OrderStatus.Completed)
            {
                TempData["ErrorMessage"] = "You can only review dishes from completed orders.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            bool alreadyReviewed = await _context.Reviews.AnyAsync(r => r.OrderItemId == model.OrderItemId);
            if (alreadyReviewed)
            {
                TempData["ErrorMessage"] = "You've already reviewed this dish.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            if (model.Rating < 1 || model.Rating > 5)
            {
                TempData["ErrorMessage"] = "Please choose a rating between 1 and 5 stars.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            _context.Reviews.Add(new Review
            {
                OrderItemId = model.OrderItemId,
                MenuItemId = orderItem.MenuItemId,
                CustomerId = CurrentUserId,
                Rating = model.Rating,
                Comment = model.Comment
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thanks for your review!";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}
