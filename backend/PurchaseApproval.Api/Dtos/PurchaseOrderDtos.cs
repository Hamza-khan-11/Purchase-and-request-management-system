using System.ComponentModel.DataAnnotations;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Models;

namespace PurchaseApproval.Api.Dtos;

// One row in the orders list
public class PurchaseOrderListItemDto
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public int PurchaseRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime PromisedDeliveryDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public bool IsOverdue { get; set; }
}

public class OrderItemDto
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

// What the current user may do with this order right now
public class OrderPermissionsDto
{
    public bool CanSend { get; set; }
    public bool CanDeliver { get; set; }
    public bool CanCancel { get; set; }
}

public class PurchaseOrderDetailDto : PurchaseOrderListItemDto
{
    public string VendorCode { get; set; } = string.Empty;
    public decimal RequestEstimatedTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public OrderPermissionsDto Permissions { get; set; } = new();
}

public class PurchaseOrderCreateInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a purchase request.")]
    public int PurchaseRequestId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a vendor.")]
    public int VendorId { get; set; }

    [Required(ErrorMessage = "Promised delivery date is required.")]
    public DateTime? PromisedDeliveryDate { get; set; }

    // Filled in by the screen when the officer used the AI recommendation
    public int? RecommendationLogId { get; set; }

    [StringLength(1000, ErrorMessage = "The reason can be at most 1000 characters.")]
    public string? OverrideReason { get; set; }
}

public class DeliverInput
{
    // Empty means "today"
    public DateTime? DeliveredDate { get; set; }
}

// ?page=1&pageSize=10&search=PO-2026&status=Sent&vendorId=3&from=2026-10-01&to=2026-10-31&sortBy=amount
public class PurchaseOrderQuery : PagedQuery
{
    public PurchaseOrderStatus? Status { get; set; }
    public int? VendorId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

// A vendor that could fulfil a request, with what it would cost
public class VendorOptionDto
{
    public int VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public decimal TotalPrice { get; set; }
    public int LeadTimeDays { get; set; }
}

