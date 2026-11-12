namespace PurchaseApproval.Api.Models;

public class Approval : BaseEntity
{
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest PurchaseRequest { get; set; } = null!;
    public int ApproverId { get; set; }
    public User Approver { get; set; } = null!;
    public ApprovalDecision Decision { get; set; }
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; }
}
