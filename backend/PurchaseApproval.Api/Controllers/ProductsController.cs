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
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetAll([FromQuery] ProductQuery query) =>
        Ok(ApiResponse<PagedResult<ProductDto>>.Ok(await _service.GetPagedAsync(query)));

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> GetCategories() =>
        Ok(ApiResponse<IReadOnlyList<string>>.Ok(await _service.GetCategoriesAsync()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id) =>
        Ok(ApiResponse<ProductDto>.Ok(await _service.GetByIdAsync(id)));

    [HttpPost]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(ProductRequest request)
    {
        var created = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<ProductDto>.Ok(created, "Product created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(int id, ProductRequest request) =>
        Ok(ApiResponse<ProductDto>.Ok(await _service.UpdateAsync(id, request), "Product updated."));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.OfficerOrAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
