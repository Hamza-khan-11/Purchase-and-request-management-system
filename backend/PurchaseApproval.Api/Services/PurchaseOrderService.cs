using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Constants;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Models;
using PurchaseApproval.Api.Repositories;

namespace PurchaseApproval.Api.Services;

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderListItemDto>> GetPagedAsync(PurchaseOrderQuery query);
    Task<PurchaseOrderDetailDto> GetByIdAsync(int id);
    Task<List<VendorOptionDto>> GetVendorOptionsAsync(int purchaseRequestId);
    Task<PurchaseOrderDetailDto> CreateAsync(PurchaseOrderCreateInput input);
    Task<PurchaseOrderDetailDto> SendAsync(int id);
    Task<PurchaseOrderDetailDto> DeliverAsync(int id, DateTime? deliveredDate);
    Task<PurchaseOrderDetailDto> CancelAsync(int id);
}

public class PurchaseOrderService : IPurchaseOrderService
{
    private const string ValidationMessage = "Validation failed. Please check the highlighted fields.";

    private readonly IRepository<PurchaseOrder> _orders;
    private readonly IRepository<PurchaseRequest> _requests;
    private readonly IRepository<Product> _products;
    private readonly IVendorQuoteService _quotes;
    private readonly ICurrentUserService _currentUser;
    private readonly IRecommendationService? _recommendations;

    public PurchaseOrderService(
        IRepository<PurchaseOrder> orders,
        IRepository<PurchaseRequest> requests,
        IRepository<Product> products,
        IVendorQuoteService quotes,
        ICurrentUserService currentUser,
        IRecommendationService? recommendations = null)
    {
        _orders = orders;
        _requests = requests;
        _products = products;
        _quotes = quotes;
        _currentUser = currentUser;
        _recommendations = recommendations;
    }

    // ---------- Reading ----------

    public async Task<PagedResult<PurchaseOrderListItemDto>> GetPagedAsync(PurchaseOrderQuery q)
    {
        var today = DateTime.UtcNow.Date;
        var query = _orders.Query();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(o => o.PoNumber.Contains(s) || o.PurchaseRequest.RequestNumber.Contains(s)
                                     || o.PurchaseRequest.Title.Contains(s) || o.Vendor.Name.Contains(s));
        }

        if (q.Status.HasValue)
        {
            var status = q.Status.Value;
            query = query.Where(o => o.Status == status);
        }

        if (q.VendorId.HasValue)
        {
            var vendorId = q.VendorId.Value;
            query = query.Where(o => o.VendorId == vendorId);
        }

        if (q.From.HasValue)
        {
            var from = q.From.Value.Date;
            query = query.Where(o => o.OrderDate >= from);
        }

        if (q.To.HasValue)
        {
            var toExclusive = q.To.Value.Date.AddDays(1);
            query = query.Where(o => o.OrderDate < toExclusive);
        }

        return await ApplySort(query, q.SortBy, q.Descending)
            .ThenBy(o => o.Id)
            .Select(ListProjection(today))
            .ToPagedResultAsync(q.Page, q.PageSize);
    }

    public async Task<PurchaseOrderDetailDto> GetByIdAsync(int id)
    {
        var order = await _orders.Query()
            .Include(o => o.Vendor)
            .Include(o => o.PurchaseRequest).ThenInclude(r => r.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException($"Purchase order {id} was not found.");

        // Product names are read ignoring soft delete, so old orders stay readable
        var productIds = order.PurchaseRequest.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _products.Query().IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        return ToDetail(order, products);
    }

    public async Task<List<VendorOptionDto>> GetVendorOptionsAsync(int purchaseRequestId)
    {
        var exists = await _requests.Query()
            .AnyAsync(r => r.Id == purchaseRequestId && r.Status != RequestStatus.Draft);
        if (!exists)
            throw new NotFoundException($"Purchase request {purchaseRequestId} was not found.");

        return await _quotes.GetEligibleVendorsAsync(purchaseRequestId);
    }

    // ---------- Writing ----------

    public async Task<PurchaseOrderDetailDto> CreateAsync(PurchaseOrderCreateInput input)
    {
        var request = await _requests.Query(tracking: true).FirstOrDefaultAsync(r => r.Id == input.PurchaseRequestId);
        if (request == null || request.Status == RequestStatus.Draft)
            throw new NotFoundException($"Purchase request {input.PurchaseRequestId} was not found.");

        if (request.Status != RequestStatus.Approved)
            throw new ConflictException($"This request is {request.Status}. Only approved requests can become a purchase order.");

        var today = DateTime.UtcNow.Date;
        var promised = input.PromisedDeliveryDate!.Value.Date;
        if (promised < today)
            throw Validation("PromisedDeliveryDate", "The promised delivery date cannot be in the past.");

        var options = await _quotes.GetEligibleVendorsAsync(request.Id);
        var option = options.FirstOrDefault(o => o.VendorId == input.VendorId)
            ?? throw Validation("VendorId", "This vendor is inactive or does not quote every item of this request.");

        // The database has a unique index for this too, but this gives a clear message
        var hasActiveOrder = await _orders.Query()
            .AnyAsync(o => o.PurchaseRequestId == request.Id && o.Status != PurchaseOrderStatus.Cancelled);
        if (hasActiveOrder)
            throw new ConflictException("This request already has an active purchase order.");

        // Check and record the officer's choice against the AI analysis (saved together with the order)
        if (input.RecommendationLogId.HasValue && _recommendations != null)
        {
            await _recommendations.ApplyDecisionAsync(
                input.RecommendationLogId.Value, request.Id, option.VendorId, input.OverrideReason);
        }

        var order = new PurchaseOrder
        {
            PoNumber = await NextPoNumberAsync(),
            PurchaseRequestId = request.Id,
            VendorId = option.VendorId,
            OrderDate = today,
            PromisedDeliveryDate = promised,
            TotalAmount = option.TotalPrice,
            Status = PurchaseOrderStatus.Created
        };
        request.Status = RequestStatus.PoCreated;

        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();   // the order and the request status are saved together
        return await GetByIdAsync(order.Id);
    }

    public async Task<PurchaseOrderDetailDto> SendAsync(int id)
    {
        var order = await LoadAsync(id);
        EnsureStatus(order, "sent", PurchaseOrderStatus.Created);

        order.Status = PurchaseOrderStatus.Sent;
        await _orders.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<PurchaseOrderDetailDto> DeliverAsync(int id, DateTime? deliveredDate)
    {
        var order = await LoadAsync(id);
        EnsureStatus(order, "marked as delivered", PurchaseOrderStatus.Created, PurchaseOrderStatus.Sent);

        var today = DateTime.UtcNow.Date;
        var date = (deliveredDate ?? today).Date;
        if (date > today)
            throw Validation("DeliveredDate", "The delivery date cannot be in the future.");
        if (date < order.OrderDate.Date)
            throw Validation("DeliveredDate", "The delivery date cannot be before the order date.");

        order.DeliveredDate = date;
        order.Status = PurchaseOrderStatus.Delivered;
        await _orders.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<PurchaseOrderDetailDto> CancelAsync(int id)
    {
        var order = await LoadAsync(id);
        EnsureStatus(order, "cancelled", PurchaseOrderStatus.Created, PurchaseOrderStatus.Sent);

        order.Status = PurchaseOrderStatus.Cancelled;

        // The request goes back to Approved so a new order can be made
        var request = await _requests.Query(tracking: true).FirstOrDefaultAsync(r => r.Id == order.PurchaseRequestId);
        if (request != null && request.Status == RequestStatus.PoCreated)
            request.Status = RequestStatus.Approved;

        await _orders.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    // ---------- Helpers ----------

    private async Task<PurchaseOrder> LoadAsync(int id) =>
        await _orders.Query(tracking: true).FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException($"Purchase order {id} was not found.");

    private static void EnsureStatus(PurchaseOrder order, string action, params PurchaseOrderStatus[] allowed)
    {
        if (!allowed.Contains(order.Status))
            throw new ConflictException($"This purchase order is {order.Status} and cannot be {action}.");
    }

    private static AppException Validation(string field, string message) =>
        new(ValidationMessage, 400, new Dictionary<string, string[]> { [field] = new[] { message } });

    // PO-2026-0039: one running number, taken from the newest order (cancelled ones included)
    private async Task<string> NextPoNumberAsync()
    {
        var last = await _orders.Query().IgnoreQueryFilters()
            .OrderByDescending(o => o.Id)
            .Select(o => o.PoNumber)
            .FirstOrDefaultAsync();

        var next = 1;
        if (last != null && int.TryParse(last[(last.LastIndexOf('-') + 1)..], out var number))
            next = number + 1;

        return $"PO-{DateTime.UtcNow.Year}-{next:0000}";
    }

    private static Expression<Func<PurchaseOrder, PurchaseOrderListItemDto>> ListProjection(DateTime today) =>
        o => new PurchaseOrderListItemDto
        {
            Id = o.Id,
            PoNumber = o.PoNumber,
            PurchaseRequestId = o.PurchaseRequestId,
            RequestNumber = o.PurchaseRequest.RequestNumber,
            RequestTitle = o.PurchaseRequest.Title,
            VendorId = o.VendorId,
            VendorName = o.Vendor.Name,
            OrderDate = o.OrderDate,
            PromisedDeliveryDate = o.PromisedDeliveryDate,
            DeliveredDate = o.DeliveredDate,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            IsOverdue = (o.Status == PurchaseOrderStatus.Created || o.Status == PurchaseOrderStatus.Sent)
                        && o.PromisedDeliveryDate < today
        };

    private PurchaseOrderDetailDto ToDetail(PurchaseOrder o, Dictionary<int, Product> products)
    {
        var canManage = _currentUser.Role == Roles.ProcurementOfficer || _currentUser.Role == Roles.Admin;
        var open = o.Status == PurchaseOrderStatus.Created || o.Status == PurchaseOrderStatus.Sent;

        return new PurchaseOrderDetailDto
        {
            Id = o.Id,
            PoNumber = o.PoNumber,
            PurchaseRequestId = o.PurchaseRequestId,
            RequestNumber = o.PurchaseRequest.RequestNumber,
            RequestTitle = o.PurchaseRequest.Title,
            VendorId = o.VendorId,
            VendorName = o.Vendor.Name,
            VendorCode = o.Vendor.Code,
            OrderDate = o.OrderDate,
            PromisedDeliveryDate = o.PromisedDeliveryDate,
            DeliveredDate = o.DeliveredDate,
            TotalAmount = o.TotalAmount,
            RequestEstimatedTotal = o.PurchaseRequest.TotalAmount,
            Status = o.Status,
            IsOverdue = open && o.PromisedDeliveryDate < DateTime.UtcNow.Date,
            CreatedAt = o.CreatedAt,
            Items = o.PurchaseRequest.Items
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.Id)
                .Select(i =>
                {
                    products.TryGetValue(i.ProductId, out var p);
                    return new OrderItemDto
                    {
                        ProductSku = p?.Sku ?? string.Empty,
                        ProductName = p?.Name ?? "(removed product)",
                        Unit = p?.Unit ?? string.Empty,
                        Quantity = i.Quantity,
                        Notes = i.Notes
                    };
                }).ToList(),
            Permissions = new OrderPermissionsDto
            {
                CanSend = canManage && o.Status == PurchaseOrderStatus.Created,
                CanDeliver = canManage && open,
                CanCancel = canManage && open
            }
        };
    }

    private static IOrderedQueryable<PurchaseOrder> ApplySort(IQueryable<PurchaseOrder> query, string? sortBy, bool desc)
    {
        return (sortBy?.ToLowerInvariant(), desc) switch
        {
            ("number", false) => query.OrderBy(o => o.PoNumber),
            ("number", true) => query.OrderByDescending(o => o.PoNumber),
            ("request", false) => query.OrderBy(o => o.PurchaseRequest.RequestNumber),
            ("request", true) => query.OrderByDescending(o => o.PurchaseRequest.RequestNumber),
            ("vendor", false) => query.OrderBy(o => o.Vendor.Name),
            ("vendor", true) => query.OrderByDescending(o => o.Vendor.Name),
            ("status", false) => query.OrderBy(o => o.Status),
            ("status", true) => query.OrderByDescending(o => o.Status),
            ("amount", false) => query.OrderBy(o => o.TotalAmount),
            ("amount", true) => query.OrderByDescending(o => o.TotalAmount),
            ("promised", false) => query.OrderBy(o => o.PromisedDeliveryDate),
            ("promised", true) => query.OrderByDescending(o => o.PromisedDeliveryDate),
            ("delivered", false) => query.OrderBy(o => o.DeliveredDate),
            ("delivered", true) => query.OrderByDescending(o => o.DeliveredDate),
            ("orderdate", false) => query.OrderBy(o => o.OrderDate),
            _ => query.OrderByDescending(o => o.OrderDate)   // default: newest first
        };
    }
}

