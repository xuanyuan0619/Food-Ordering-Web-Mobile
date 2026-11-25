using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodOrderingSystem.Controllers
{
    // Module: Admin Maintenance (staff accounts, dashboard)
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            int pendingCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            ViewBag.PendingOrders = pendingCount;

            // Module: Order Analytics - last 7 days order trend
            var today = DateTime.Today;
            var startDate = today.AddDays(-6);

            var ordersInRange = await _context.Orders
                .Where(o => o.OrderDate.Date >= startDate)
                .Select(o => new { o.OrderDate, o.Status })
                .ToListAsync();

            var trendLabels = new List<string>();
            var trendCounts = new List<int>();
            for (var day = startDate; day <= today; day = day.AddDays(1))
            {
                trendLabels.Add(day.ToString("dd MMM"));
                trendCounts.Add(ordersInRange.Count(o => o.OrderDate.Date == day));
            }

            // Status distribution - Pending here must equal the "Pending Orders" stat card above
            int completedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Completed);
            int cancelledCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);

            bool hasRealData = ordersInRange.Any() || pendingCount + completedCount + cancelledCount > 0;

            if (!hasRealData)
            {
                // No orders yet in the system - show labelled sample data so the chart isn't empty
                ViewBag.IsSampleData = true;
                trendLabels.Clear();
                trendCounts.Clear();
                var sample = new[] { 3, 5, 2, 6, 4, 7, 5 };
                for (int i = 0; i < 7; i++)
                {
                    trendLabels.Add(startDate.AddDays(i).ToString("dd MMM"));
                    trendCounts.Add(sample[i]);
                }
                pendingCount = 4;
                completedCount = 18;
                cancelledCount = 2;
            }
            else
            {
                ViewBag.IsSampleData = false;
            }

            ViewBag.TrendLabels = trendLabels;
            ViewBag.TrendCounts = trendCounts;
            ViewBag.StatusPending = pendingCount;
            ViewBag.StatusCompleted = completedCount;
            ViewBag.StatusCancelled = cancelledCount;

            return View();
        }

        public async Task<IActionResult> Staff()
        {
            var staff = await _context.Users
                .Where(u => u.Role == UserRole.Staff || u.Role == UserRole.Admin)
                .OrderBy(u => u.Username)
                .ToListAsync();
            return View(staff);
        }

        // Module: Add Staff
        public IActionResult CreateStaff() => View(new CreateStaffViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError(nameof(model.Username), "Username already taken.");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email already registered.");
                return View(model);
            }

            // Only Staff or Admin accounts may be created here (Customers self-register)
            if (model.Role == UserRole.Customer)
            {
                ModelState.AddModelError(nameof(model.Role), "Please select Staff or Admin.");
                return View(model);
            }

            string salt = PasswordHelper.GenerateSalt();
            var user = new User
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                PasswordSalt = salt,
                PasswordHash = PasswordHelper.HashPassword(model.Password, salt)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{model.Role} account '{user.Username}' created.";
            return RedirectToAction(nameof(Staff));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Staff));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Staff));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "You can't delete your own account while logged in.";
                return RedirectToAction(nameof(Staff));
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Orders reference the user with Restrict delete behavior, so block deletion if they have order history.
            bool hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == id);
            if (hasOrders)
            {
                TempData["ErrorMessage"] = "This account has order history and can't be deleted. Deactivate it instead.";
                return RedirectToAction(nameof(Staff));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Account '{user.Username}' deleted.";
            return RedirectToAction(nameof(Staff));
        }
    }
}
