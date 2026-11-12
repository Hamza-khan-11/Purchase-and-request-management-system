namespace PurchaseApproval.Api.Models;

public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal StandardPrice { get; set; }

    public ICollection<VendorProduct> VendorProducts { get; set; } = new List<VendorProduct>();
    public ICollection<RequestItem> RequestItems { get; set; } = new List<RequestItem>();
}
