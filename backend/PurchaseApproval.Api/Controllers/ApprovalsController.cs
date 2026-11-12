using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Constants;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Services;

namespace PurchaseApproval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.ManagerOrAdmin)]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _service;

    public ApprovalsController(IApprovalService service) => _service = service;

    [HttpPost("{requestId:int}/approve")]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> Approve(int requestId, [FromBody] ApprovalDecisionInput? input) =>
        Ok(ApiResponse<PurchaseRequestDetailDto>.Ok(await _service.ApproveAsync(requestId, input?.Comments), "Request approved."));

    [HttpPost("{requestId:int}/reject")]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> Reject(int requestId, [FromBody] ApprovalDecisionInput? input) =>
        Ok(ApiResponse<PurchaseRequestDetailDto>.Ok(await _service.RejectAsync(requestId, input?.Comments), "Request rejected."));
}
