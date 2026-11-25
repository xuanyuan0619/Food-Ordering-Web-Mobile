using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any()) return; // already seeded

            // --- Users ---
            string adminSalt = PasswordHelper.GenerateSalt();
            string staffSalt = PasswordHelper.GenerateSalt();
            string custSalt = PasswordHelper.GenerateSalt();

            var admin = new User
            {
                Username = "admin",
                PasswordSalt = adminSalt,
                PasswordHash = PasswordHelper.HashPassword("Admin@123", adminSalt),
                FullName = "System Administrator",
                Email = "admin@foodorder.local",
                Role = UserRole.Admin
            };
            var staff = new User
            {
                Username = "staff1",
                PasswordSalt = staffSalt,
                PasswordHash = PasswordHelper.HashPassword("Staff@123", staffSalt),
                FullName = "Kitchen Staff",
                Email = "staff1@foodorder.local",
                Role = UserRole.Staff
            };
            var customer = new User
            {
                Username = "customer1",
                PasswordSalt = custSalt,
                PasswordHash = PasswordHelper.HashPassword("Cust@123", custSalt),
                FullName = "Tan Wei Ming",
                Email = "customer1@foodorder.local",
                Role = UserRole.Customer,
                PointsBalance = 150,
                LifetimePoints = 150
            };

            context.Users.AddRange(admin, staff, customer);

            // --- Categories & Menu Items ---
            var mains = new Category { CategoryName = "Main Course", DisplayOrder = 1 };
            var drinks = new Category { CategoryName = "Beverages", DisplayOrder = 2 };
            var desserts = new Category { CategoryName = "Desserts", DisplayOrder = 3 };

            context.Categories.AddRange(mains, drinks, desserts);

            context.MenuItems.AddRange(
                new MenuItem { ItemName = "Chicken Chop", Price = 15.90m, Category = mains, IsSignatureDish = true, Description = "Grilled chicken chop with black pepper sauce" },
                new MenuItem { ItemName = "Nasi Lemak", Price = 8.50m, Category = mains, Description = "Coconut rice with sambal, egg and anchovies" },
                new MenuItem { ItemName = "Iced Lemon Tea", Price = 4.50m, Category = drinks },
                new MenuItem { ItemName = "Fresh Orange Juice", Price = 5.50m, Category = drinks },
                new MenuItem { ItemName = "Chocolate Lava Cake", Price = 9.90m, Category = desserts, IsSignatureDish = true }
            );

            // --- Dining Tables ---
            context.DiningTables.AddRange(
                new DiningTable { TableNumber = "T1", Capacity = 2 },
                new DiningTable { TableNumber = "T2", Capacity = 4 },
                new DiningTable { TableNumber = "T3", Capacity = 6 }
            );

            // --- Vouchers (Discount module) ---
            context.VoucherCodes.AddRange(
                new VoucherCode { Code = "WELCOME5", DiscountType = DiscountType.FixedAmount, DiscountValue = 5.00m, MinSpend = 20.00m, ExpiryDate = DateTime.Now.AddMonths(6) },
                new VoucherCode { Code = "SAVE10", DiscountType = DiscountType.Percentage, DiscountValue = 10.00m, MinSpend = 30.00m, ExpiryDate = DateTime.Now.AddMonths(6) }
            );

            // --- Reward catalog (Points redemption module) ---
            context.RewardItems.AddRange(
                new RewardItem { Name = "Free Iced Lemon Tea", Description = "Redeem a free drink", PointsCost = 100 },
                new RewardItem { Name = "RM5 Off Voucher", Description = "Redeem for a RM5 discount on your next order", PointsCost = 200 },
                new RewardItem { Name = "Free Chocolate Lava Cake", Description = "Redeem a free dessert", PointsCost = 300 }
            );

            context.SaveChanges();
        }
    }
}
