using System.Security.Claims;
using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Common;

public interface ICurrentUser
{
    int Id { get; }
    UserRole Role { get; }
}

/// <summary>Reads the logged-in user's id and role from the JWT claims.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("No HTTP context.");

    public int Id => int.Parse(Principal.FindFirstValue(AppClaims.UserId)
        ?? throw new UnauthorizedAccessException("Missing user id claim."));

    public UserRole Role => Enum.Parse<UserRole>(Principal.FindFirstValue(AppClaims.Role)
        ?? throw new UnauthorizedAccessException("Missing role claim."));
}

public static class AppClaims
{
    public const string UserId = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
}
