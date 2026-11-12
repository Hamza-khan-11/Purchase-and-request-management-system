using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Data;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Models;
using PurchaseApproval.Api.Repositories;

namespace PurchaseApproval.Api.Services;

public interface IRecommendationService
{
    // Ranks the vendors that could fulfil an approved request, and saves the analysis in the log
    Task<RecommendationResultDto> EvaluateAsync(int purchaseRequestId);

    // Called while an order is created. Checks the officer's final choice against a saved analysis
    // and updates its log row. It does not save: the caller saves, together with the order.
    Task ApplyDecisionAsync(int logId, int purchaseRequestId, int finalVendorId, string? overrideReason);
}

public class RecommendationService : IRecommendationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IRepository<PurchaseRequest> _requests;
    private readonly IRepository<PurchaseOrder> _orders;
    private readonly IVendorQuoteService _quotes;
    private readonly RecommendationOptions _options;
    private readonly ICurrentUserService _currentUser;

    public RecommendationService(
        AppDbContext db,
        IRepository<PurchaseRequest> requests,
        IRepository<PurchaseOrder> orders,
        IVendorQuoteService quotes,
        IOptions<RecommendationOptions> options,
        ICurrentUserService currentUser)
    {
        _db = db;
        _requests = requests;
        _orders = orders;
        _quotes = quotes;
        _options = options.Value;
        _currentUser = currentUser;
    }

    private sealed record Scored(VendorOptionDto Option, int Orders, int OnTime, decimal Price, decimal Delivery, decimal Rating, decimal Total);

    public async Task<RecommendationResultDto> EvaluateAsync(int purchaseRequestId)
    {
        var userId = _currentUser.UserId ?? throw new AppException("You are not logged in.", 401);

        var request = await _requests.Query()
            .Where(r => r.Id == purchaseRequestId && r.Status != RequestStatus.Draft)
            .Select(r => new { r.Id, r.RequestNumber, r.Status })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Purchase request {purchaseRequestId} was not found.");

        if (request.Status != RequestStatus.Approved)
            throw new ConflictException($"This request is {request.Status}. Recommendations are only made for approved requests.");

        var weightSum = _options.PriceWeight + _options.DeliveryWeight + _options.RatingWeight;
        if (weightSum <= 0)
            throw new InvalidOperationException("The recommendation weights must add up to more than zero.");

        var result = new RecommendationResultDto
        {
            PurchaseRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            GeneratedAt = DateTime.UtcNow,
            Weights = new RecommendationWeightsDto
            {
                Price = Math.Round(_options.PriceWeight / weightSum, 2),
                Delivery = Math.Round(_options.DeliveryWeight / weightSum, 2),
                Rating = Math.Round(_options.RatingWeight / weightSum, 2)
            }
        };

        var eligible = await _quotes.GetEligibleVendorsAsync(request.Id);
        if (eligible.Count == 0)
        {
            result.Summary = "No active vendor quotes every item of this request, so no recommendation can be made.";
            return result;
        }

        var history = await LoadHistoryAsync(eligible.Select(v => v.VendorId).ToList());
        var cheapest = eligible.Min(v => v.TotalPrice);

        var scored = eligible
            .Select(option =>
            {
                history.TryGetValue(option.VendorId, out var h);
                var price = VendorScorer.PriceScore(option.TotalPrice, cheapest);
                var delivery = VendorScorer.DeliveryScore(h.OnTime, h.Orders);
                var rating = VendorScorer.RatingScore(option.Rating);
                var total = VendorScorer.TotalScore(price, delivery, rating, _options);
                return new Scored(option, h.Orders, h.OnTime, price, delivery, rating, total);
            })
            .OrderByDescending(s => s.Total)
            .ThenBy(s => s.Option.TotalPrice)
            .ThenBy(s => s.Option.VendorName)
            .ToList();

        var top = scored[0];
        var second = scored.Count > 1 ? scored[1] : null;

        var (confidenceScore, confidenceLabel) =
            VendorScorer.Confidence(top.Orders, top.Total, second?.Total, _options);

        result.Confidence = confidenceLabel;
        result.ConfidenceScore = confidenceScore;
        result.Summary = second == null
            ? $"Recommended: {top.Option.VendorName}. It is the only vendor that can supply every item."
            : $"Recommended: {top.Option.VendorName} with {F1(top.Total)} out of 100, "
              + $"{F1(top.Total - second.Total)} points ahead of {second.Option.VendorName}. "
              + $"Its biggest advantage is {Dominant(top, second)}.";

        result.Vendors = scored
            .Select((s, index) => new VendorRecommendationDto
            {
                Rank = index + 1,
                IsRecommended = index == 0,
                VendorId = s.Option.VendorId,
                VendorCode = s.Option.VendorCode,
                VendorName = s.Option.VendorName,
                TotalPrice = s.Option.TotalPrice,
                LeadTimeDays = s.Option.LeadTimeDays,
                Rating = s.Option.Rating,
                OrdersConsidered = s.Orders,
                OnTimeOrders = s.OnTime,
                PriceScore = Math.Round(s.Price, 4),
                DeliveryScore = Math.Round(s.Delivery, 4),
                RatingScore = Math.Round(s.Rating, 4),
                TotalScore = s.Total,
                Explanation = Explain(index + 1, scored.Count, s, cheapest)
            })
            .ToList();

        // Keep a record of what was shown, so any later decision can be explained
        var log = new AiRecommendationLog
        {
            PurchaseRequestId = request.Id,
            RequestedBy = userId,
            InputJson = JsonSerializer.Serialize(new
            {
                purchaseRequestId = request.Id,
                requestNumber = request.RequestNumber,
                weights = result.Weights,
                historyDays = _options.HistoryDays,
                eligibleVendorIds = eligible.Select(v => v.VendorId).ToList()
            }, JsonOptions),
            OutputJson = JsonSerializer.Serialize(result, JsonOptions),
            RecommendedVendorId = top.Option.VendorId,
            CreatedAt = result.GeneratedAt
        };
        _db.AiRecommendationLogs.Add(log);
        await _db.SaveChangesAsync();

        result.LogId = log.Id;
        return result;
    }

    public async Task ApplyDecisionAsync(int logId, int purchaseRequestId, int finalVendorId, string? overrideReason)
    {
        var log = await _db.AiRecommendationLogs.FirstOrDefaultAsync(l => l.Id == logId)
            ?? throw Validation("RecommendationLogId", "The analysis was not found. Run it again.");

        if (log.PurchaseRequestId != purchaseRequestId)
            throw Validation("RecommendationLogId", "This analysis belongs to a different request. Run it again.");

        if (log.FinalVendorId != null)
            throw new ConflictException("This analysis has already been used for an order. Run it again.");

        var overridden = log.RecommendedVendorId != finalVendorId;
        var reason = string.IsNullOrWhiteSpace(overrideReason) ? null : overrideReason.Trim();
        if (overridden && reason == null)
            throw Validation("OverrideReason", "Please explain why you are not choosing the recommended vendor.");

        log.FinalVendorId = finalVendorId;
        log.WasOverridden = overridden;
        log.OverrideReason = overridden ? reason : null;
    }

    // ---------- Helpers ----------

    // Per vendor: how many recent orders count, and how many of them were on time.
    // Delivered orders count. Open orders already past their promised date count as late.
    // Cancelled orders are ignored.
    private async Task<Dictionary<int, (int Orders, int OnTime)>> LoadHistoryAsync(List<int> vendorIds)
    {
        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(-_options.HistoryDays);

        var rows = await _orders.Query()
            .Where(o => vendorIds.Contains(o.VendorId)
                        && o.OrderDate >= cutoff
                        && (o.Status == PurchaseOrderStatus.Delivered
                            || ((o.Status == PurchaseOrderStatus.Created || o.Status == PurchaseOrderStatus.Sent)
                                && o.PromisedDeliveryDate < today)))
            .Select(o => new { o.VendorId, o.Status, o.PromisedDeliveryDate, o.DeliveredDate })
            .ToListAsync();

        return rows
            .GroupBy(r => r.VendorId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Orders: g.Count(),
                    OnTime: g.Count(r => r.Status == PurchaseOrderStatus.Delivered
                                         && r.DeliveredDate != null
                                         && r.DeliveredDate.Value.Date <= r.PromisedDeliveryDate.Date)));
    }

    private static string Explain(int rank, int count, Scored s, decimal cheapest)
    {
        var price = s.Option.TotalPrice <= cheapest
            ? "lowest total price"
            : cheapest <= 0
                ? "higher total price"
                : $"total price {F1((s.Option.TotalPrice / cheapest - 1m) * 100m)}% above the cheapest";

        var delivery = s.Orders == 0
            ? "no delivery history yet (a neutral score was used)"
            : $"{s.OnTime} of {s.Orders} recent orders delivered on time";

        var rating = $"rating {F1(s.Option.Rating)} out of 5";

        return $"Ranked {Ordinal(rank)} of {count} with {F1(s.Total)} out of 100: {price}, {delivery}, {rating}.";
    }

    // Which factor gives the winner its biggest weighted lead over the runner-up
    private string Dominant(Scored top, Scored second)
    {
        var sum = _options.PriceWeight + _options.DeliveryWeight + _options.RatingWeight;
        var price = _options.PriceWeight / sum * (top.Price - second.Price);
        var delivery = _options.DeliveryWeight / sum * (top.Delivery - second.Delivery);
        var rating = _options.RatingWeight / sum * (top.Rating - second.Rating);

        var best = Math.Max(price, Math.Max(delivery, rating));
        if (best <= 0) return "its overall balance";
        if (best == price) return "its lower price";
        if (best == delivery) return "its delivery record";
        return "its higher rating";
    }

    private static string Ordinal(int n)
    {
        var suffix = (n % 100) is 11 or 12 or 13
            ? "th"
            : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{n}{suffix}";
    }

    private static string F1(decimal value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    private static AppException Validation(string field, string message) =>
        new("Validation failed. Please check the highlighted fields.", 400,
            new Dictionary<string, string[]> { [field] = new[] { message } });
}
