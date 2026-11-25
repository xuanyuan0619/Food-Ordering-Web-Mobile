using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    // Module: Table & Reservation Maintenance
    [Authorize]
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = nameof(UserRole.Customer))]
        public async Task<IActionResult> Create()
        {
            ViewBag.Tables = await _context.DiningTables.ToListAsync();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            ViewBag.CurrentUser = user;

            return View(new ReservationViewModel());
        }

        [Authorize(Roles = nameof(UserRole.Customer))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservationViewModel model)
        {
            var table = await _context.DiningTables.FindAsync(model.TableId);
            if (table == null)
            {
                ModelState.AddModelError(nameof(model.TableId), "Selected table does not exist.");
            }
            else if (model.PartySize > table.Capacity)
            {
                ModelState.AddModelError(nameof(model.PartySize), $"Table {table.TableNumber} only seats {table.Capacity}.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Tables = await _context.DiningTables.ToListAsync();
                ViewBag.CurrentUser = await _context.Users.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
                return View(model);
            }

            var reservation = new Reservation
            {
                ContactName = model.ContactName,
                ContactPhone = model.ContactPhone,
                TableId = model.TableId,
                PartySize = model.PartySize,
                ReservationDateTime = model.ReservationDateTime,
                SpecialRequest = model.SpecialRequest,
                CustomerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                Status = ReservationStatus.Pending,
                CreatedDate = DateTime.Now
            };

            _context.Reservations.Add(reservation);
            if (table != null) table.Status = TableStatus.Reserved;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Reservation submitted. Awaiting confirmation.";
            return RedirectToAction(nameof(MyReservations));
        }

        [Authorize(Roles = nameof(UserRole.Customer))]
        public async Task<IActionResult> MyReservations()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var reservations = await _context.Reservations
                .Include(r => r.Table)
                .Where(r => r.CustomerId == userId)
                .OrderByDescending(r => r.ReservationDateTime)
                .ToListAsync();
            return View(reservations);
        }

        // Staff/Admin manage all reservations
        [Authorize(Roles = $"{nameof(UserRole.Staff)},{nameof(UserRole.Admin)}")]
        public async Task<IActionResult> Manage(string? search)
        {
            var query = _context.Reservations
                .Include(r => r.Table)
                .Include(r => r.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(r =>
                    r.ContactName.Contains(term) ||
                    (r.Customer != null && r.Customer.FullName.Contains(term)));
            }

            var reservations = await query.OrderBy(r => r.ReservationDateTime).ToListAsync();

            ViewBag.Search = search;
            return View(reservations);
        }

        [Authorize(Roles = $"{nameof(UserRole.Staff)},{nameof(UserRole.Admin)}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ReservationStatus status)
        {
            var reservation = await _context.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationId == id);
            if (reservation == null) return NotFound();

            reservation.Status = status;

            if (status == ReservationStatus.Cancelled && reservation.Table != null)
                reservation.Table.Status = TableStatus.Available;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }
    }
}
