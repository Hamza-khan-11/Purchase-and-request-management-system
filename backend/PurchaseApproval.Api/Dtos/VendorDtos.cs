using System.ComponentModel.DataAnnotations;
using PurchaseApproval.Api.Common;

namespace PurchaseApproval.Api.Dtos;

// What the API sends back
public class VendorDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal Rating { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// What the API accepts for both create and update
public class VendorRequest
{
    [Required(ErrorMessage = "Vendor code is required.")]
    [StringLength(20, ErrorMessage = "Vendor code can be at most 20 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vendor name is required.")]
    [StringLength(200, ErrorMessage = "Vendor name can be at most 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Contact person can be at most 150 characters.")]
    public string? ContactPerson { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email can be at most 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Phone can be at most 30 characters.")]
    public string? Phone { get; set; }

    [StringLength(500, ErrorMessage = "Address can be at most 500 characters.")]
    public string? Address { get; set; }

    [Range(0.0, 5.0, ErrorMessage = "Rating must be between 0 and 5.")]
    public decimal Rating { get; set; }

    public bool IsActive { get; set; } = true;
}

// Query string for the list: ?page=1&pageSize=10&search=tech&sortBy=name&descending=false&isActive=true
public class VendorQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}
