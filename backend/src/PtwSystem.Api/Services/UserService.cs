using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PtwSystem.Api.Common;
using PtwSystem.Api.Data;
using PtwSystem.Api.Domain.Entities;
using PtwSystem.Api.Dtos;

namespace PtwSystem.Api.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto> SetActiveAsync(int id, bool isActive);
}

public class UserService(AppDbContext db, IPasswordHasher<User> hasher) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync() =>
        await db.Users.AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email,
                Role = u.Role, Company = u.Company, IsActive = u.IsActive
            })
            .ToListAsync();

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new BusinessRuleException($"A user with email {email} already exists.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = request.Role,
            Company = request.Company.Trim()
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.ToDto();
    }

    public async Task<UserDto> SetActiveAsync(int id, bool isActive)
    {
        var user = await db.Users.FindAsync(id) ?? throw new NotFoundException("User not found.");
        user.IsActive = isActive;
        await db.SaveChangesAsync();
        return user.ToDto();
    }
}
