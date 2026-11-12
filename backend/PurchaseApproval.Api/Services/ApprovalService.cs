using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Constants;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Models;
using PurchaseApproval.Api.Repositories;

namespace PurchaseApproval.Api.Services;

public interface IApprovalService
{
    Task<PurchaseRequestDetailDto> ApproveAsync(int requestId, string? comments);
    Task<PurchaseRequestDetailDto> RejectAsync(int requestId, string? comments);
}

public class ApprovalService : IApprovalService
{
    private readonly IRepository<PurchaseRequest> _requests;
    private readonly ICurrentUserService _currentUser;
    private readonly IPurchaseRequestService _requestService;

    public ApprovalService(
        IRepository<PurchaseRequest> requests,
        ICurrentUserService currentUser,
        IPurchaseRequestService requestService)
    {
        _requests = requests;
        _currentUser = currentUser;
        _requestService = requestService;
    }

    public Task<PurchaseRequestDetailDto> ApproveAsync(int requestId, string? comments) =>
        DecideAsync(requestId, ApprovalDecision.Approved, comments);

    public Task<PurchaseRequestDetailDto> RejectAsync(int requestId, string? comments) =>
        DecideAsync(requestId, ApprovalDecision.Rejected, comments);

    private async Task<PurchaseRequestDetailDto> DecideAsync(int requestId, ApprovalDecision decision, string? comments)
    {
        var userId = _currentUser.UserId ?? throw new AppException("You are not logged in.", 401);

        var request = await _requests.Query(tracking: true).FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new NotFoundException($"Purchase request {requestId} was not found.");

        // Other people's drafts are private. Only an admin may even see that they exist.
        if (request.Status == RequestStatus.Draft && request.RequesterId != userId && _currentUser.Role != Roles.Admin)
            throw new NotFoundException($"Purchase request {requestId} was not found.");

        if (request.RequesterId == userId)
            throw new ForbiddenException("You cannot approve or reject your own request.");

        if (request.Status != RequestStatus.Submitted)
            throw new ConflictException($"This request is {request.Status}. Only submitted requests can be approved or rejected.");

        var cleaned = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
        if (decision == ApprovalDecision.Rejected && cleaned == null)
        {
            throw new AppException("Validation failed. Please check the highlighted fields.", 400,
                new Dictionary<string, string[]>
                {
                    ["Comments"] = new[] { "A comment is required when rejecting a request." }
                });
        }

        request.Status = decision == ApprovalDecision.Approved ? RequestStatus.Approved : RequestStatus.Rejected;
        request.Approvals.Add(new Approval
        {
            ApproverId = userId,
            Decision = decision,
            Comments = cleaned,
            DecidedAt = DateTime.UtcNow
        });

        await _requests.SaveChangesAsync();
        return await _requestService.GetByIdAsync(requestId);
    }
}
