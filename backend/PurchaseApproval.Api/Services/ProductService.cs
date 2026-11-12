using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Common;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Models;
using PurchaseApproval.Api.Repositories;

namespace PurchaseApproval.Api.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery query);
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<ProductDto> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(ProductRequest request);
    Task<ProductDto> UpdateAsync(int id, ProductRequest request);
    Task DeleteAsync(int id);
}

public class ProductService : IProductService
{
    private readonly IRepository<Product> _products;
    private readonly IRepository<RequestItem> _items;

    public ProductService(IRepository<Product> products, IRepository<RequestItem> items)
    {
        _products = products;
        _items = items;
    }

    private static readonly Expression<Func<Product, ProductDto>> Projection = p => new ProductDto
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        Category = p.Category,
        Unit = p.Unit,
        StandardPrice = p.StandardPrice,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };

    private static readonly Func<Product, ProductDto> ToDto = Projection.Compile();

    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery q)
    {
        var query = _products.Query();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(p => p.Name.Contains(s) || p.Sku.Contains(s) || p.Category.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(q.Category))
        {
            var category = q.Category.Trim();
            query = query.Where(p => p.Category == category);
        }

        if (q.IsActive.HasValue)
        {
            var active = q.IsActive.Value;
            query = query.Where(p => p.IsActive == active);
        }

        return await ApplySort(query, q.SortBy, q.Descending)
            .ThenBy(p => p.Id)
            .Select(Projection)
            .ToPagedResultAsync(q.Page, q.PageSize);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync() =>
        await _products.Query().Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync();

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product = await _products.Query().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Product {id} was not found.");
        return ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(ProductRequest request)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();
        await EnsureUniqueAsync(sku, 0);

        var product = new Product
        {
            Sku = sku,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Unit = request.Unit.Trim(),
            StandardPrice = request.StandardPrice,
            IsActive = request.IsActive
        };

        await _products.AddAsync(product);
        await _products.SaveChangesAsync();
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, ProductRequest request)
    {
        var product = await _products.GetByIdAsync(id)
            ?? throw new NotFoundException($"Product {id} was not found.");

        var sku = request.Sku.Trim().ToUpperInvariant();
        await EnsureUniqueAsync(sku, id);

        product.Sku = sku;
        product.Name = request.Name.Trim();
        product.Category = request.Category.Trim();
        product.Unit = request.Unit.Trim();
        product.StandardPrice = request.StandardPrice;
        product.IsActive = request.IsActive;

        await _products.SaveChangesAsync();
        return ToDto(product);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _products.GetByIdAsync(id)
            ?? throw new NotFoundException($"Product {id} was not found.");

        // Requests that are still in progress need this product
        var usedInOpenRequest = await _items.Query().AnyAsync(i =>
            i.ProductId == id &&
            (i.PurchaseRequest.Status == RequestStatus.Draft ||
             i.PurchaseRequest.Status == RequestStatus.Submitted ||
             i.PurchaseRequest.Status == RequestStatus.Approved));
        if (usedInOpenRequest)
            throw new ConflictException("This product is used in open purchase requests and cannot be deleted.");

        _products.Remove(product);
        await _products.SaveChangesAsync();
    }

    private async Task EnsureUniqueAsync(string sku, int excludeId)
    {
        if (await _products.Query().AnyAsync(p => p.Sku == sku && p.Id != excludeId))
        {
            throw new ConflictException("A product with the same SKU already exists.",
                new Dictionary<string, string[]> { ["Sku"] = new[] { $"SKU '{sku}' is already in use." } });
        }
    }

    private static IOrderedQueryable<Product> ApplySort(IQueryable<Product> query, string? sortBy, bool desc)
    {
        return (sortBy?.ToLowerInvariant(), desc) switch
        {
            ("sku", false) => query.OrderBy(p => p.Sku),
            ("sku", true) => query.OrderByDescending(p => p.Sku),
            ("category", false) => query.OrderBy(p => p.Category),
            ("category", true) => query.OrderByDescending(p => p.Category),
            ("price", false) => query.OrderBy(p => p.StandardPrice),
            ("price", true) => query.OrderByDescending(p => p.StandardPrice),
            ("createdat", false) => query.OrderBy(p => p.CreatedAt),
            ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
            (_, true) => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };
    }
}
