using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Constants;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Services;

namespace PurchaseApproval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]   // every endpoint needs a logged-in user; some also need a role
public class VendorsController : ControllerBase
{
    private readonly IVendorService _service;

    public VendorsController(IVendorService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<VendorDto>>>> GetAll([FromQuery] VendorQuery query) =>
        Ok(ApiResponse<PagedResult<VendorDto>>.Ok(await _service.GetPagedAsync(query)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<VendorDto>>> GetById(int id) =>
        Ok(ApiResponse<VendorDto>.Ok(await _service.GetByIdAsync(id)));

    [HttpPost]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<VendorDto>>> Create(VendorRequest request)
    {
        var created = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<VendorDto>.Ok(created, "Vendor created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<VendorDto>>> Update(int id, VendorRequest request) =>
        Ok(ApiResponse<VendorDto>.Ok(await _service.UpdateAsync(id, request), "Vendor updated."));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
