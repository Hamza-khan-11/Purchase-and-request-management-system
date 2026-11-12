using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Models;
using PurchaseApproval.Api.Repositories;

namespace PurchaseApproval.Api.Services;

public interface IVendorService
{
    Task<PagedResult<VendorDto>> GetPagedAsync(VendorQuery query);
    Task<VendorDto> GetByIdAsync(int id);
    Task<VendorDto> CreateAsync(VendorRequest request);
    Task<VendorDto> UpdateAsync(int id, VendorRequest request);
    Task DeleteAsync(int id);
}

public class VendorService : IVendorService
{
    private readonly IRepository<Vendor> _vendors;
    private readonly IRepository<PurchaseOrder> _orders;

    public VendorService(IRepository<Vendor> vendors, IRepository<PurchaseOrder> orders)
    {
        _vendors = vendors;
        _orders = orders;
    }

    // One mapping used for lists (translated to SQL) and for single records
    private static readonly Expression<Func<Vendor, VendorDto>> Projection = v => new VendorDto
    {
        Id = v.Id,
        Code = v.Code,
        Name = v.Name,
        ContactPerson = v.ContactPerson,
        Email = v.Email,
        Phone = v.Phone,
        Address = v.Address,
        Rating = v.Rating,
        IsActive = v.IsActive,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };

    private static readonly Func<Vendor, VendorDto> ToDto = Projection.Compile();

    public async Task<PagedResult<VendorDto>> GetPagedAsync(VendorQuery q)
    {
        var query = _vendors.Query();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(v => v.Name.Contains(s) || v.Code.Contains(s) || v.Email.Contains(s)
                                     || (v.ContactPerson != null && v.ContactPerson.Contains(s)));
        }

        if (q.IsActive.HasValue)
        {
            var active = q.IsActive.Value;
            query = query.Where(v => v.IsActive == active);
        }

        return await ApplySort(query, q.SortBy, q.Descending)
            .ThenBy(v => v.Id)   // stable order, so pages never repeat or skip rows
            .Select(Projection)
            .ToPagedResultAsync(q.Page, q.PageSize);
    }

    public async Task<VendorDto> GetByIdAsync(int id)
    {
        var vendor = await _vendors.Query().FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException($"Vendor {id} was not found.");
        return ToDto(vendor);
    }

    public async Task<VendorDto> CreateAsync(VendorRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var email = request.Email.Trim();
        await EnsureUniqueAsync(code, email, 0);

        var vendor = new Vendor
        {
            Code = code,
            Name = request.Name.Trim(),
            ContactPerson = Clean(request.ContactPerson),
            Email = email,
            Phone = Clean(request.Phone),
            Address = Clean(request.Address),
            Rating = request.Rating,
            IsActive = request.IsActive
        };

        await _vendors.AddAsync(vendor);
        await _vendors.SaveChangesAsync();
        return ToDto(vendor);
    }

    public async Task<VendorDto> UpdateAsync(int id, VendorRequest request)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vendor {id} was not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        var email = request.Email.Trim();
        await EnsureUniqueAsync(code, email, id);

        vendor.Code = code;
        vendor.Name = request.Name.Trim();
        vendor.ContactPerson = Clean(request.ContactPerson);
        vendor.Email = email;
        vendor.Phone = Clean(request.Phone);
        vendor.Address = Clean(request.Address);
        vendor.Rating = request.Rating;
        vendor.IsActive = request.IsActive;

        await _vendors.SaveChangesAsync();
        return ToDto(vendor);
    }

    public async Task DeleteAsync(int id)
    {
        var vendor = await _vendors.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vendor {id} was not found.");

        var hasOpenOrders = await _orders.Query().AnyAsync(o =>
            o.VendorId == id &&
            (o.Status == PurchaseOrderStatus.Created || o.Status == PurchaseOrderStatus.Sent));
        if (hasOpenOrders)
            throw new ConflictException("This vendor has open purchase orders and cannot be deleted.");

        _vendors.Remove(vendor);   // becomes a soft delete
        await _vendors.SaveChangesAsync();
    }

    // The database also has unique indexes, but this check gives a friendly message per field
    private async Task EnsureUniqueAsync(string code, string email, int excludeId)
    {
        var errors = new Dictionary<string, string[]>();

        if (await _vendors.Query().AnyAsync(v => v.Code == code && v.Id != excludeId))
            errors["Code"] = new[] { $"Vendor code '{code}' is already in use." };

        if (await _vendors.Query().AnyAsync(v => v.Email == email && v.Id != excludeId))
            errors["Email"] = new[] { "This email is already used by another vendor." };

        if (errors.Count > 0)
            throw new ConflictException("A vendor with the same code or email already exists.", errors);
    }

    // Only these columns can be sorted. Never sort by a raw column name from the browser.
    private static IOrderedQueryable<Vendor> ApplySort(IQueryable<Vendor> query, string? sortBy, bool desc)
    {
        return (sortBy?.ToLowerInvariant(), desc) switch
        {
            ("code", false) => query.OrderBy(v => v.Code),
            ("code", true) => query.OrderByDescending(v => v.Code),
            ("email", false) => query.OrderBy(v => v.Email),
            ("email", true) => query.OrderByDescending(v => v.Email),
            ("rating", false) => query.OrderBy(v => v.Rating),
            ("rating", true) => query.OrderByDescending(v => v.Rating),
            ("createdat", false) => query.OrderBy(v => v.CreatedAt),
            ("createdat", true) => query.OrderByDescending(v => v.CreatedAt),
            (_, true) => query.OrderByDescending(v => v.Name),
            _ => query.OrderBy(v => v.Name)
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
