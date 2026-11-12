using Microsoft.AspNetCore.Identity;
using PurchaseApproval.Api.Dtos;
using PurchaseApproval.Api.Models;
using PurchaseApproval.Api.Repositories;

namespace PurchaseApproval.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task LogoutAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IAuditService _audit;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(IUserRepository users, ITokenService tokens, IAuditService audit)
    {
        _users = users;
        _tokens = tokens;
        _audit = audit;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email.Trim());

        // Same result for "no such user", "inactive" and "wrong password"
        // so an attacker cannot learn which emails exist.
        if (user == null || !user.IsActive)
        {
            await _audit.LogAsync(null, "LoginFailed", "User", null);
            return null;
        }

        var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (check == PasswordVerificationResult.Failed)
        {
            await _audit.LogAsync(user.Id, "LoginFailed", "User", user.Id);
            return null;
        }

        var (token, expiresAt) = _tokens.CreateToken(user);
        await _audit.LogAsync(user.Id, "Login", "User", user.Id);

        return new LoginResponse(token, expiresAt,
            new UserInfo(user.Id, user.FullName, user.Email, user.Role.ToString()));
    }

    public Task LogoutAsync(int userId) =>
        _audit.LogAsync(userId, "Logout", "User", userId);
}
