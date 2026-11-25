namespace FoodOrderingSystem.ViewModels
{
    // Module: Top Selling - simple DTO for the ranked list shown on the Report page
    public class TopSellingItem
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
}
