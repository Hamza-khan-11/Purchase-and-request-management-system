using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Constants;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Services;

namespace PurchaseApproval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseRequestsController : ControllerBase
{
    private readonly IPurchaseRequestService _service;

    public PurchaseRequestsController(IPurchaseRequestService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseRequestListItemDto>>>> GetAll([FromQuery] PurchaseRequestQuery query) =>
        Ok(ApiResponse<PagedResult<PurchaseRequestListItemDto>>.Ok(await _service.GetPagedAsync(query)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> GetById(int id) =>
        Ok(ApiResponse<PurchaseRequestDetailDto>.Ok(await _service.GetByIdAsync(id)));

    [HttpPost]
    [Authorize(Roles = Roles.RequesterOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> Create(PurchaseRequestInput input)
    {
        var created = await _service.CreateAsync(input);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<PurchaseRequestDetailDto>.Ok(created, "Request saved as a draft."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.RequesterOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> Update(int id, PurchaseRequestInput input) =>
        Ok(ApiResponse<PurchaseRequestDetailDto>.Ok(await _service.UpdateAsync(id, input), "Request updated."));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.RequesterOrAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = Roles.RequesterOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> Submit(int id) =>
        Ok(ApiResponse<PurchaseRequestDetailDto>.Ok(await _service.SubmitAsync(id), "Request submitted for approval."));

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = Roles.RequesterOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> Cancel(int id) =>
        Ok(ApiResponse<PurchaseRequestDetailDto>.Ok(await _service.CancelAsync(id), "Request cancelled."));

    [HttpPost("{id:int}/revise")]
    [Authorize(Roles = Roles.RequesterOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> Revise(int id) =>
        Ok(ApiResponse<PurchaseRequestDetailDto>.Ok(await _service.ReviseAsync(id), "Request reopened as a draft."));
}
