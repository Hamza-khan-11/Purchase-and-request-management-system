using System.ComponentModel.DataAnnotations;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Models;

namespace PurchaseApproval.Api.Dtos;

// One row in the list screen
public class PurchaseRequestListItemDto
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public int RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class RequestItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

public class ApprovalDto
{
    public int Id { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public ApprovalDecision Decision { get; set; }
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; }
}

// What the current user may do with this request right now
public class RequestPermissionsDto
{
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanCancel { get; set; }
    public bool CanRevise { get; set; }
    public bool CanDecide { get; set; }
    public bool CanCreateOrder { get; set; }
}

// The detail screen
public class PurchaseRequestDetailDto
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Justification { get; set; }
    public RequestStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public List<RequestItemDto> Items { get; set; } = new();
    public List<ApprovalDto> Approvals { get; set; } = new();
    public RequestPermissionsDto Permissions { get; set; } = new();
}

// What the browser sends for one item: only the product and the quantity, never a price
public class PurchaseRequestItemInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a product.")]
    public int ProductId { get; set; }

    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000.")]
    public int Quantity { get; set; }

    [StringLength(500, ErrorMessage = "Notes can be at most 500 characters.")]
    public string? Notes { get; set; }
}

// What the browser sends for create and update
public class PurchaseRequestInput
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, ErrorMessage = "Title can be at most 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Justification can be at most 1000 characters.")]
    public string? Justification { get; set; }

    public DateTime? RequiredByDate { get; set; }

    public List<PurchaseRequestItemInput> Items { get; set; } = new();
}

// ?page=1&pageSize=10&search=laptop&status=Draft&from=2026-10-01&to=2026-10-31&sortBy=amount&descending=true
public class PurchaseRequestQuery : PagedQuery
{
    public RequestStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool? AwaitingMyApproval { get; set; }
}


