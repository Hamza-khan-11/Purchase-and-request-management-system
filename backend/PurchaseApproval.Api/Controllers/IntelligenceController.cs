using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Constants;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Services;

namespace PurchaseApproval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.OfficerOrAdmin)]
public class IntelligenceController : ControllerBase
{
    private readonly IRecommendationService _service;

    public IntelligenceController(IRecommendationService service) => _service = service;

    // Ranks the vendors for an approved request. This is advice only: nothing is ordered here.
    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<RecommendationResultDto>>> Evaluate(EvaluateInput input) =>
        Ok(ApiResponse<RecommendationResultDto>.Ok(await _service.EvaluateAsync(input.PurchaseRequestId), "Analysis complete."));
}
