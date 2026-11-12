using System.ComponentModel.DataAnnotations;

namespace PurchaseApproval.Api.Dtos;

public class EvaluateInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a purchase request.")]
    public int PurchaseRequestId { get; set; }
}

// The weights actually used, scaled so they add up to 1
public class RecommendationWeightsDto
{
    public decimal Price { get; set; }
    public decimal Delivery { get; set; }
    public decimal Rating { get; set; }
}

public class VendorRecommendationDto
{
    public int Rank { get; set; }
    public bool IsRecommended { get; set; }
    public int VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal Rating { get; set; }

    // The facts behind the delivery score
    public int OrdersConsidered { get; set; }
    public int OnTimeOrders { get; set; }

    // Each between 0 and 1
    public decimal PriceScore { get; set; }
    public decimal DeliveryScore { get; set; }
    public decimal RatingScore { get; set; }

    // Between 0 and 100
    public decimal TotalScore { get; set; }

    public string Explanation { get; set; } = string.Empty;
}

public class RecommendationResultDto
{
    // The saved log row. Null when there was nothing to recommend.
    public int? LogId { get; set; }
    public int PurchaseRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public RecommendationWeightsDto Weights { get; set; } = new();
    public string Confidence { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Disclaimer { get; set; } =
        "This is a suggestion based on price, past delivery and rating. The final decision is yours, and every choice is logged.";
    public List<VendorRecommendationDto> Vendors { get; set; } = new();
}
