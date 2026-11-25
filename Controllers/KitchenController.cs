using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    // Module: Kitchen & Order Fulfillment
    [Authorize(Roles = $"{nameof(UserRole.Staff)},{nameof(UserRole.Admin)}")]
    public class KitchenController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KitchenController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Active order queue for the kitchen display
        public async Task<IActionResult> Queue()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status, string? staffNote)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            bool justCompleted = status == OrderStatus.Completed && order.Status != OrderStatus.Completed;

            order.Status = status;
            order.StaffNote = staffNote;

            // Free up the table once the order is completed or cancelled
            if ((status == OrderStatus.Completed || status == OrderStatus.Cancelled) && order.Table != null)
            {
                order.Table.Status = TableStatus.Available;
            }

            // Module: Membership & Points - award points once, on first completion, and settle cash payment
            if (justCompleted && order.Customer != null)
            {
                int pointsEarned = (int)Math.Floor(order.TotalAmount);
                order.PointsEarned = pointsEarned;
                order.Customer.PointsBalance += pointsEarned;
                order.Customer.LifetimePoints += pointsEarned;
                order.PaymentStatus = PaymentStatus.Paid;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Queue));
        }

        // Module: Reporting (basic order history / sales report for staff & admin)
        public async Task<IActionResult> Report(DateTime? from, DateTime? to)
        {
            var query = _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem).AsQueryable();

            if (from.HasValue) query = query.Where(o => o.OrderDate >= from.Value);
            if (to.HasValue) query = query.Where(o => o.OrderDate <= to.Value);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            ViewBag.TotalRevenue = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount);
            ViewBag.From = from;
            ViewBag.To = to;

            // Module: Top Selling - rank menu items by quantity sold within the filtered date range
            ViewBag.TopSelling = orders
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.MenuItem != null)
                .GroupBy(oi => new { oi.MenuItemId, oi.MenuItem!.ItemName })
                .Select(g => new TopSellingItem
                {
                    MenuItemId = g.Key.MenuItemId,
                    ItemName = g.Key.ItemName,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Subtotal)
                })
                .OrderByDescending(t => t.QuantitySold)
                .Take(5)
                .ToList();

            return View(orders);
        }

        // Module: Order Report Detail (admin/staff view of a single order's full breakdown)
        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
