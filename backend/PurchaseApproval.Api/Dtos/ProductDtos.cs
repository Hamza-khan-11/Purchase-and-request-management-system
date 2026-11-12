using System.ComponentModel.DataAnnotations;
using PurchaseApproval.Api.Common;

namespace PurchaseApproval.Api.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal StandardPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductRequest
{
    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(50, ErrorMessage = "SKU can be at most 50 characters.")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200, ErrorMessage = "Product name can be at most 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(100, ErrorMessage = "Category can be at most 100 characters.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit is required.")]
    [StringLength(30, ErrorMessage = "Unit can be at most 30 characters.")]
    public string Unit { get; set; } = string.Empty;

    [Range(0.0, 1000000000.0, ErrorMessage = "Price must be between 0 and 1000000000.")]
    public decimal StandardPrice { get; set; }

    public bool IsActive { get; set; } = true;
}

// ?page=1&pageSize=10&search=paper&category=Stationery&isActive=true&sortBy=price&descending=true
public class ProductQuery : PagedQuery
{
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
}
