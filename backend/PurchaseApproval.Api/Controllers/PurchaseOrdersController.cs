using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Constants;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Services;

namespace PurchaseApproval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.ManagerOfficerOrAdmin)]   // managers may look; changes need more (see below)
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrdersController(IPurchaseOrderService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderListItemDto>>>> GetAll([FromQuery] PurchaseOrderQuery query) =>
        Ok(ApiResponse<PagedResult<PurchaseOrderListItemDto>>.Ok(await _service.GetPagedAsync(query)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> GetById(int id) =>
        Ok(ApiResponse<PurchaseOrderDetailDto>.Ok(await _service.GetByIdAsync(id)));

    [HttpGet("vendor-options")]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<List<VendorOptionDto>>>> GetVendorOptions([FromQuery] int requestId) =>
        Ok(ApiResponse<List<VendorOptionDto>>.Ok(await _service.GetVendorOptionsAsync(requestId)));

    [HttpPost]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> Create(PurchaseOrderCreateInput input)
    {
        var created = await _service.CreateAsync(input);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<PurchaseOrderDetailDto>.Ok(created, "Purchase order created."));
    }

    [HttpPost("{id:int}/send")]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> Send(int id) =>
        Ok(ApiResponse<PurchaseOrderDetailDto>.Ok(await _service.SendAsync(id), "Purchase order marked as sent."));

    [HttpPost("{id:int}/deliver")]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> Deliver(int id, [FromBody] DeliverInput? input) =>
        Ok(ApiResponse<PurchaseOrderDetailDto>.Ok(await _service.DeliverAsync(id, input?.DeliveredDate), "Purchase order marked as delivered."));

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> Cancel(int id) =>
        Ok(ApiResponse<PurchaseOrderDetailDto>.Ok(await _service.CancelAsync(id), "Purchase order cancelled."));
}
