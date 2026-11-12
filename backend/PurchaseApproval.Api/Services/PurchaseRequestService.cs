using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Constants;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Models;
using PurchaseApproval.Api.Repositories;

namespace PurchaseApproval.Api.Services;

public interface IPurchaseRequestService
{
    Task<PagedResult<PurchaseRequestListItemDto>> GetPagedAsync(PurchaseRequestQuery query);
    Task<PurchaseRequestDetailDto> GetByIdAsync(int id);
    Task<PurchaseRequestDetailDto> CreateAsync(PurchaseRequestInput input);
    Task<PurchaseRequestDetailDto> UpdateAsync(int id, PurchaseRequestInput input);
    Task DeleteAsync(int id);
    Task<PurchaseRequestDetailDto> SubmitAsync(int id);
    Task<PurchaseRequestDetailDto> CancelAsync(int id);
    Task<PurchaseRequestDetailDto> ReviseAsync(int id);
}

public class PurchaseRequestService : IPurchaseRequestService
{
    private const string ValidationMessage = "Validation failed. Please check the highlighted fields.";

    private readonly IRepository<PurchaseRequest> _requests;
    private readonly IRepository<Product> _products;
    private readonly ICurrentUserService _currentUser;

    public PurchaseRequestService(
        IRepository<PurchaseRequest> requests,
        IRepository<Product> products,
        ICurrentUserService currentUser)
    {
        _requests = requests;
        _products = products;
        _currentUser = currentUser;
    }

    private int UserId => _currentUser.UserId ?? throw new AppException("You are not logged in.", 401);

    // ---------- Reading ----------

    // Which requests the current user is allowed to see
    private IQueryable<PurchaseRequest> VisibleRequests(bool tracking = false)
    {
        var userId = UserId;
        var query = _requests.Query(tracking);

        return _currentUser.Role switch
        {
            Roles.Admin => query,
            Roles.Manager or Roles.ProcurementOfficer =>
                query.Where(r => r.RequesterId == userId || r.Status != RequestStatus.Draft),
            _ => query.Where(r => r.RequesterId == userId)
        };
    }

    public async Task<PagedResult<PurchaseRequestListItemDto>> GetPagedAsync(PurchaseRequestQuery q)
    {
        var query = VisibleRequests();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(r => r.RequestNumber.Contains(s) || r.Title.Contains(s) || r.Requester.FullName.Contains(s));
        }

        if (q.Status.HasValue)
        {
            var status = q.Status.Value;
            query = query.Where(r => r.Status == status);
        }

        if (q.AwaitingMyApproval == true)
        {
            var me = UserId;
            query = query.Where(r => r.Status == RequestStatus.Submitted && r.RequesterId != me);
        }

        if (q.From.HasValue)
        {
            var from = q.From.Value.Date;
            query = query.Where(r => r.CreatedAt >= from);
        }

        if (q.To.HasValue)
        {
            var toExclusive = q.To.Value.Date.AddDays(1);   // "to" includes the whole day
            query = query.Where(r => r.CreatedAt < toExclusive);
        }

        return await ApplySort(query, q.SortBy, q.Descending)
            .ThenBy(r => r.Id)
            .Select(r => new PurchaseRequestListItemDto
            {
                Id = r.Id,
                RequestNumber = r.RequestNumber,
                Title = r.Title,
                Status = r.Status,
                TotalAmount = r.TotalAmount,
                ItemCount = r.Items.Count(i => !i.IsDeleted),
                RequiredByDate = r.RequiredByDate,
                RequesterId = r.RequesterId,
                RequesterName = r.Requester.FullName,
                CreatedAt = r.CreatedAt,
                SubmittedAt = r.SubmittedAt
            })
            .ToPagedResultAsync(q.Page, q.PageSize);
    }

    public async Task<PurchaseRequestDetailDto> GetByIdAsync(int id)
    {
        // Not visible to this user = "not found" (so nobody can probe which numbers exist)
        var request = await VisibleRequests()
            .Include(r => r.Requester)
            .Include(r => r.Items)
            .Include(r => r.Approvals).ThenInclude(a => a.Approver)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Purchase request {id} was not found.");

        // Product details are read separately, ignoring soft delete, so old requests
        // still show the name of a product that was removed later.
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _products.Query().IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        return ToDetail(request, products);
    }

    // ---------- Writing ----------

    public async Task<PurchaseRequestDetailDto> CreateAsync(PurchaseRequestInput input)
    {
        var userId = UserId;
        ValidateHeader(input);
        var lines = await BuildLinesAsync(input.Items);

        var request = new PurchaseRequest
        {
            RequestNumber = await NextRequestNumberAsync(),
            RequesterId = userId,
            Title = input.Title.Trim(),
            Justification = Clean(input.Justification),
            Status = RequestStatus.Draft,
            RequiredByDate = input.RequiredByDate?.Date,
            TotalAmount = lines.Sum(l => l.LineTotal)
        };
        foreach (var line in lines) request.Items.Add(line);

        await _requests.AddAsync(request);
        await _requests.SaveChangesAsync();
        return await GetByIdAsync(request.Id);
    }

    public async Task<PurchaseRequestDetailDto> UpdateAsync(int id, PurchaseRequestInput input)
    {
        var request = await LoadForChangeAsync(id);
        EnsureStatus(request, "edited", RequestStatus.Draft);
        ValidateHeader(input);
        var lines = await BuildLinesAsync(input.Items);

        request.Title = input.Title.Trim();
        request.Justification = Clean(input.Justification);
        request.RequiredByDate = input.RequiredByDate?.Date;
        request.TotalAmount = lines.Sum(l => l.LineTotal);

        // Match the old rows and the new rows by product
        var wanted = lines.ToDictionary(l => l.ProductId);
        foreach (var existing in request.Items.Where(i => !i.IsDeleted).ToList())
        {
            if (wanted.TryGetValue(existing.ProductId, out var line))
            {
                existing.Quantity = line.Quantity;
                existing.EstimatedUnitPrice = line.EstimatedUnitPrice;
                existing.LineTotal = line.LineTotal;
                existing.Notes = line.Notes;
                wanted.Remove(existing.ProductId);
            }
            else
            {
                existing.IsDeleted = true;   // the user removed this row
            }
        }
        foreach (var line in wanted.Values) request.Items.Add(line);   // brand new rows

        await _requests.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var request = await LoadForChangeAsync(id);
        EnsureStatus(request, "deleted", RequestStatus.Draft);

        foreach (var item in request.Items) item.IsDeleted = true;
        _requests.Remove(request);   // soft delete
        await _requests.SaveChangesAsync();
    }

    public async Task<PurchaseRequestDetailDto> SubmitAsync(int id)
    {
        var request = await LoadForChangeAsync(id);
        EnsureStatus(request, "submitted", RequestStatus.Draft);

        if (!request.Items.Any(i => !i.IsDeleted))
            throw new AppException("Add at least one item before submitting.", 400);

        request.Status = RequestStatus.Submitted;
        request.SubmittedAt = DateTime.UtcNow;
        await _requests.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<PurchaseRequestDetailDto> CancelAsync(int id)
    {
        var request = await LoadForChangeAsync(id);
        EnsureStatus(request, "cancelled", RequestStatus.Draft, RequestStatus.Submitted);

        request.Status = RequestStatus.Cancelled;
        await _requests.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<PurchaseRequestDetailDto> ReviseAsync(int id)
    {
        var request = await LoadForChangeAsync(id);
        EnsureStatus(request, "revised", RequestStatus.Rejected);

        request.Status = RequestStatus.Draft;
        request.SubmittedAt = null;
        await _requests.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    // ---------- Helpers ----------

    // 404 if the user cannot see it, 403 if it is not theirs
    private async Task<PurchaseRequest> LoadForChangeAsync(int id)
    {
        var request = await VisibleRequests(tracking: true)
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Purchase request {id} was not found.");

        if (request.RequesterId != UserId)
            throw new ForbiddenException("Only the person who created this request can change it.");

        return request;
    }

    private static void EnsureStatus(PurchaseRequest request, string action, params RequestStatus[] allowed)
    {
        if (!allowed.Contains(request.Status))
            throw new ConflictException($"This request is {request.Status} and cannot be {action}.");
    }

    private static void ValidateHeader(PurchaseRequestInput input)
    {
        // One day of tolerance because the user's "today" can differ from UTC "today"
        if (input.RequiredByDate.HasValue && input.RequiredByDate.Value.Date < DateTime.UtcNow.Date.AddDays(-1))
        {
            throw new AppException(ValidationMessage, 400, new Dictionary<string, string[]>
            {
                ["RequiredByDate"] = new[] { "The required-by date cannot be in the past." }
            });
        }
    }

    // Turns the browser's (product, quantity) rows into real rows with server-side prices
    private async Task<List<RequestItem>> BuildLinesAsync(List<PurchaseRequestItemInput> items)
    {
        var errors = new Dictionary<string, string[]>();

        if (items.Count == 0)
        {
            throw new AppException(ValidationMessage, 400, new Dictionary<string, string[]>
            {
                ["Items"] = new[] { "Add at least one item." }
            });
        }

        if (items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
            errors["Items"] = new[] { "Each product can only be added once. Combine the quantities instead." };

        var ids = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _products.Query().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var lines = new List<RequestItem>();
        for (var index = 0; index < items.Count; index++)
        {
            var input = items[index];
            if (!products.TryGetValue(input.ProductId, out var product))
            {
                errors[$"Items[{index}].ProductId"] = new[] { "This product no longer exists." };
                continue;
            }
            if (!product.IsActive)
            {
                errors[$"Items[{index}].ProductId"] = new[] { $"{product.Name} is inactive and cannot be requested." };
                continue;
            }

            lines.Add(new RequestItem
            {
                ProductId = product.Id,
                Quantity = input.Quantity,
                EstimatedUnitPrice = product.StandardPrice,
                LineTotal = Math.Round(product.StandardPrice * input.Quantity, 2),
                Notes = Clean(input.Notes)
            });
        }

        if (errors.Count > 0) throw new AppException(ValidationMessage, 400, errors);
        return lines;
    }

    // PR-2026-0052: one running number, taken from the newest request (deleted ones included)
    private async Task<string> NextRequestNumberAsync()
    {
        var last = await _requests.Query().IgnoreQueryFilters()
            .OrderByDescending(r => r.Id)
            .Select(r => r.RequestNumber)
            .FirstOrDefaultAsync();

        var next = 1;
        if (last != null && int.TryParse(last[(last.LastIndexOf('-') + 1)..], out var number))
            next = number + 1;

        return $"PR-{DateTime.UtcNow.Year}-{next:0000}";
    }

    private PurchaseRequestDetailDto ToDetail(PurchaseRequest r, Dictionary<int, Product> products)
    {
        var isOwner = r.RequesterId == UserId;
        var isDraft = r.Status == RequestStatus.Draft;
        var items = r.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Id).ToList();

        return new PurchaseRequestDetailDto
        {
            Id = r.Id,
            RequestNumber = r.RequestNumber,
            Title = r.Title,
            Justification = r.Justification,
            Status = r.Status,
            TotalAmount = r.TotalAmount,
            RequiredByDate = r.RequiredByDate,
            SubmittedAt = r.SubmittedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            RequesterId = r.RequesterId,
            RequesterName = r.Requester.FullName,
            Items = items.Select(i =>
            {
                products.TryGetValue(i.ProductId, out var p);
                return new RequestItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductSku = p?.Sku ?? string.Empty,
                    ProductName = p?.Name ?? "(removed product)",
                    Unit = p?.Unit ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.EstimatedUnitPrice,
                    LineTotal = i.LineTotal,
                    Notes = i.Notes
                };
            }).ToList(),
            Approvals = r.Approvals
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.DecidedAt)
                .Select(a => new ApprovalDto
                {
                    Id = a.Id,
                    ApproverName = a.Approver.FullName,
                    Decision = a.Decision,
                    Comments = a.Comments,
                    DecidedAt = a.DecidedAt
                }).ToList(),
            Permissions = new RequestPermissionsDto
            {
                CanEdit = isOwner && isDraft,
                CanDelete = isOwner && isDraft,
                CanSubmit = isOwner && isDraft && items.Count > 0,
                CanCancel = isOwner && (isDraft || r.Status == RequestStatus.Submitted),
                CanRevise = isOwner && r.Status == RequestStatus.Rejected,
                CanDecide = !isOwner && r.Status == RequestStatus.Submitted &&
                            (_currentUser.Role == Roles.Manager || _currentUser.Role == Roles.Admin),
                CanCreateOrder = r.Status == RequestStatus.Approved &&
                                 (_currentUser.Role == Roles.ProcurementOfficer || _currentUser.Role == Roles.Admin)
            }
        };
    }

    private static IOrderedQueryable<PurchaseRequest> ApplySort(IQueryable<PurchaseRequest> query, string? sortBy, bool desc)
    {
        return (sortBy?.ToLowerInvariant(), desc) switch
        {
            ("number", false) => query.OrderBy(r => r.RequestNumber),
            ("number", true) => query.OrderByDescending(r => r.RequestNumber),
            ("title", false) => query.OrderBy(r => r.Title),
            ("title", true) => query.OrderByDescending(r => r.Title),
            ("status", false) => query.OrderBy(r => r.Status),
            ("status", true) => query.OrderByDescending(r => r.Status),
            ("amount", false) => query.OrderBy(r => r.TotalAmount),
            ("amount", true) => query.OrderByDescending(r => r.TotalAmount),
            ("required", false) => query.OrderBy(r => r.RequiredByDate),
            ("required", true) => query.OrderByDescending(r => r.RequiredByDate),
            ("submitted", false) => query.OrderBy(r => r.SubmittedAt),
            ("submitted", true) => query.OrderByDescending(r => r.SubmittedAt),
            ("created", false) => query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)   // default: newest first
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}


