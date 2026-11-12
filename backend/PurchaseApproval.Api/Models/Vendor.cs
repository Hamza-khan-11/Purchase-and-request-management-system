namespace PurchaseApproval.Api.Models;

public class Vendor : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal Rating { get; set; }

    public ICollection<VendorProduct> VendorProducts { get; set; } = new List<VendorProduct>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
