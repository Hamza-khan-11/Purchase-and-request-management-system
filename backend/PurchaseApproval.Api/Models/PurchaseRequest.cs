namespace PurchaseApproval.Api.Models;

public class PurchaseRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Justification { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Draft;
    public decimal TotalAmount { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public ICollection<RequestItem> Items { get; set; } = new List<RequestItem>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
