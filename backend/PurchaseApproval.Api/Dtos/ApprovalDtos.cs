using System.ComponentModel.DataAnnotations;

namespace PurchaseApproval.Api.Dtos;

public class ApprovalDecisionInput
{
    [StringLength(1000, ErrorMessage = "Comments can be at most 1000 characters.")]
    public string? Comments { get; set; }
}
