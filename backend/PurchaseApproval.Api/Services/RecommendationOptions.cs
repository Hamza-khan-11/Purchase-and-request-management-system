namespace PurchaseApproval.Api.Services;

// Read from appsettings.json, section "Recommendation"
public class RecommendationOptions
{
    public decimal PriceWeight { get; set; } = 0.40m;
    public decimal DeliveryWeight { get; set; } = 0.35m;
    public decimal RatingWeight { get; set; } = 0.25m;

    // Only orders placed within this many days count as delivery history
    public int HistoryDays { get; set; } = 365;

    // This many recent orders of the top vendor give full confidence in its delivery record
    public int OrdersForFullConfidence { get; set; } = 5;

    // A lead of this many points (out of 100) over the runner-up counts as a clear win
    public decimal ClearMarginPoints { get; set; } = 15m;
}
