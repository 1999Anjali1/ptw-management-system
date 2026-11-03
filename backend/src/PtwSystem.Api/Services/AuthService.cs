using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PtwSystem.Api.Common;
using PtwSystem.Api.Data;
using PtwSystem.Api.Domain.Entities;
using PtwSystem.Api.Dtos;

namespace PtwSystem.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UserDto> GetUserAsync(int id);
}

public class AuthService(AppDbContext db, ITokenService tokens, IPasswordHasher<User> hasher) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user is null) return null;

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        var (token, expires) = tokens.CreateToken(user);
        return new LoginResponse { Token = token, ExpiresAt = expires, User = user.ToDto() };
    }

    public async Task<UserDto> GetUserAsync(int id)
    {
        var user = await db.Users.FindAsync(id) ?? throw new NotFoundException("User not found.");
        return user.ToDto();
    }
}
