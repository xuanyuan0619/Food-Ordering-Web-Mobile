using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    // Module: Points - redeem points for items
    [Authorize(Roles = nameof(UserRole.Customer))]
    public class RewardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RewardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Index()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            ViewBag.PointsBalance = user?.PointsBalance ?? 0;

            var rewards = await _context.RewardItems
                .Where(r => r.IsActive)
                .OrderBy(r => r.PointsCost)
                .ToListAsync();

            return View(rewards);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Redeem(int rewardItemId)
        {
            var reward = await _context.RewardItems.FindAsync(rewardItemId);
            var user = await _context.Users.FindAsync(CurrentUserId);

            if (reward == null || !reward.IsActive || user == null) return NotFound();

            if (user.PointsBalance < reward.PointsCost)
            {
                TempData["ErrorMessage"] = "You don't have enough points to redeem this reward.";
                return RedirectToAction(nameof(Index));
            }

            user.PointsBalance -= reward.PointsCost;

            _context.Redemptions.Add(new Redemption
            {
                CustomerId = CurrentUserId,
                RewardItemId = reward.RewardItemId,
                PointsSpent = reward.PointsCost,
                Status = RedemptionStatus.Pending
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Redeemed '{reward.Name}'! Show this at the counter to collect it.";
            return RedirectToAction(nameof(Index));
        }
    }
}
