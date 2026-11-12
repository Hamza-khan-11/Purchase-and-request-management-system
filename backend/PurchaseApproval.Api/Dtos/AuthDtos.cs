using System.ComponentModel.DataAnnotations;

namespace PurchaseApproval.Api.Dtos;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
}

public record UserInfo(int Id, string FullName, string Email, string Role);

public record LoginResponse(string Token, DateTime ExpiresAt, UserInfo User);
