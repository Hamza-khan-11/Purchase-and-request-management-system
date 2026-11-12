namespace PurchaseApproval.Api.Services;

// The maths of the recommendation. No database, no web: just numbers in, numbers out.
public static class VendorScorer
{
    // The cheapest vendor gets 1.0. A vendor twice as dear gets 0.5.
    public static decimal PriceScore(decimal totalPrice, decimal cheapestPrice)
    {
        if (totalPrice <= 0) return 1m;
        return Clamp01(cheapestPrice / totalPrice);
    }

    // Share of recent orders delivered on time. With no history we use a neutral 0.5.
    public static decimal DeliveryScore(int onTimeOrders, int ordersConsidered) =>
        ordersConsidered == 0 ? 0.5m : Clamp01((decimal)onTimeOrders / ordersConsidered);

    public static decimal RatingScore(decimal rating) => Clamp01(rating / 5m);

    // 0 to 100. The weights are scaled, so they always count as adding up to 1.
    public static decimal TotalScore(decimal price, decimal delivery, decimal rating, RecommendationOptions weights)
    {
        var sum = weights.PriceWeight + weights.DeliveryWeight + weights.RatingWeight;
        if (sum <= 0)
            throw new InvalidOperationException("The recommendation weights must add up to more than zero.");

        var weighted = (weights.PriceWeight * price + weights.DeliveryWeight * delivery + weights.RatingWeight * rating) / sum;
        return Math.Round(100m * weighted, 1, MidpointRounding.AwayFromZero);
    }

    // How much to trust the recommendation: enough history for the top vendor and a clear lead.
    public static (decimal Score, string Label) Confidence(
        int topVendorOrders, decimal topScore, decimal? secondScore, RecommendationOptions options)
    {
        var history = options.OrdersForFullConfidence <= 0
            ? 1m
            : Clamp01((decimal)topVendorOrders / options.OrdersForFullConfidence);

        var margin = secondScore == null || options.ClearMarginPoints <= 0
            ? 1m
            : Clamp01((topScore - secondScore.Value) / options.ClearMarginPoints);

        var score = Math.Round(0.6m * history + 0.4m * margin, 2, MidpointRounding.AwayFromZero);
        var label = score >= 0.75m ? "High" : score >= 0.5m ? "Medium" : "Low";
        return (score, label);
    }

    private static decimal Clamp01(decimal value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
