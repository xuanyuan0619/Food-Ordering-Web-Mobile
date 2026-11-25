using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    // Module: Profile / Membership & Points
    [Authorize(Roles = nameof(UserRole.Customer))]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            // Next tier threshold, for a simple progress indicator
            ViewBag.NextTierThreshold = user.Tier switch
            {
                MembershipTier.Bronze => 500,
                MembershipTier.Silver => 2000,
                _ => (int?)null
            };

            var redemptions = await _context.Redemptions
                .Include(r => r.RewardItem)
                .Where(r => r.CustomerId == CurrentUserId)
                .OrderByDescending(r => r.DateRedeemed)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentRedemptions = redemptions;

            return View(user);
        }

        public async Task<IActionResult> Edit()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            return View(new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            bool emailTaken = await _context.Users
                .AnyAsync(u => u.Email == model.Email && u.UserId != CurrentUserId);
            if (emailTaken)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already in use.");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordSalt, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
                return View(model);
            }

            string newSalt = PasswordHelper.GenerateSalt();
            user.PasswordSalt = newSalt;
            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword, newSalt);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
