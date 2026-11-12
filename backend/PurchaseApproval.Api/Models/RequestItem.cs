namespace PurchaseApproval.Api.Models;

public class RequestItem : BaseEntity
{
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest PurchaseRequest { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal EstimatedUnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}
