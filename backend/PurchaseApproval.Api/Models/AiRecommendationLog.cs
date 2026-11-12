namespace PurchaseApproval.Api.Models;

public class AiRecommendationLog
{
    public int Id { get; set; }
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest PurchaseRequest { get; set; } = null!;
    public int RequestedBy { get; set; }
    public string InputJson { get; set; } = string.Empty;
    public string OutputJson { get; set; } = string.Empty;
    public int? RecommendedVendorId { get; set; }
    public int? FinalVendorId { get; set; }
    public bool WasOverridden { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
