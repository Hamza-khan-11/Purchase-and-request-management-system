namespace PurchaseApproval.Api.Models;

public class PurchaseOrder : BaseEntity
{
    public string PoNumber { get; set; } = string.Empty;
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest PurchaseRequest { get; set; } = null!;
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public DateTime PromisedDeliveryDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Created;
}
