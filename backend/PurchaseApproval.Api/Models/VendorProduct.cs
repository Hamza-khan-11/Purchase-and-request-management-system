namespace PurchaseApproval.Api.Models;

public class VendorProduct : BaseEntity
{
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal QuotedPrice { get; set; }
    public int LeadTimeDays { get; set; }
}
