using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Models;
using PurchaseApproval.Api.Repositories;

namespace PurchaseApproval.Api.Services;

public interface IVendorQuoteService
{
    // Active vendors that quote EVERY product of the request, cheapest first
    Task<List<VendorOptionDto>> GetEligibleVendorsAsync(int purchaseRequestId);
}

public class VendorQuoteService : IVendorQuoteService
{
    private readonly IRepository<RequestItem> _items;
    private readonly IRepository<VendorProduct> _quotes;

    public VendorQuoteService(IRepository<RequestItem> items, IRepository<VendorProduct> quotes)
    {
        _items = items;
        _quotes = quotes;
    }

    public async Task<List<VendorOptionDto>> GetEligibleVendorsAsync(int purchaseRequestId)
    {
        var items = await _items.Query()
            .Where(i => i.PurchaseRequestId == purchaseRequestId)
            .Select(i => new { i.ProductId, i.Quantity })
            .ToListAsync();
        if (items.Count == 0) return new List<VendorOptionDto>();

        var quantities = items.ToDictionary(i => i.ProductId, i => i.Quantity);
        var productIds = quantities.Keys.ToList();

        var quotes = await _quotes.Query()
            .Where(q => productIds.Contains(q.ProductId) && q.IsActive && q.Vendor.IsActive)
            .Select(q => new
            {
                q.VendorId,
                q.ProductId,
                q.QuotedPrice,
                q.LeadTimeDays,
                VendorCode = q.Vendor.Code,
                VendorName = q.Vendor.Name,
                q.Vendor.Rating
            })
            .ToListAsync();

        // A vendor is eligible only when it has a quote for every requested product
        return quotes
            .GroupBy(q => q.VendorId)
            .Where(g => g.Select(q => q.ProductId).Distinct().Count() == productIds.Count)
            .Select(g => new VendorOptionDto
            {
                VendorId = g.Key,
                VendorCode = g.First().VendorCode,
                VendorName = g.First().VendorName,
                Rating = g.First().Rating,
                TotalPrice = Math.Round(g.Sum(q => q.QuotedPrice * quantities[q.ProductId]), 2),
                LeadTimeDays = g.Max(q => q.LeadTimeDays)
            })
            .OrderBy(v => v.TotalPrice)
            .ThenBy(v => v.VendorName)
            .ToList();
    }
}
