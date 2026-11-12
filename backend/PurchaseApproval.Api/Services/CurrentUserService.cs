using System.Security.Claims;

namespace PurchaseApproval.Api.Services;

// Tells any class "who is the logged-in user right now?"
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Role { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public int? UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
}
