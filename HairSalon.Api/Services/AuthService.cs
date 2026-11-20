using HairSalon.Api.Infrastructure;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Identity;

namespace HairSalon.Api.Services;

/// <summary>
/// Registration and login with ASP.NET Core Identity.
/// UserManager does the hard work: hashing passwords, checking them, storing users and roles.
/// </summary>
public class AuthService(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService,
    NotificationService notificationService,
    LoyaltyService loyaltyService)
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await userManager.FindByEmailAsync(dto.Email) != null)
            throw new BusinessRuleException("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            PhoneNumber = dto.PhoneNumber,
            RegistrationDate = DateTime.Now
        };

        // CreateAsync hashes the password - the plain password is never stored.
        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new BusinessRuleException(string.Join(" ", result.Errors.Select(e => e.Description)));

        // Everyone who registers is a customer. Admins are created by the seeder.
        await userManager.AddToRoleAsync(user, Roles.Customer);

        await notificationService.NotifyUserAsync(user.Id, "Welcome to Nordic Hair Studio!",
            "Thank you for creating an account. You can now book appointments online.", NotificationType.General);

        // Log the new user in straight away by returning a token.
        return tokenService.CreateToken(user, new List<string> { Roles.Customer });
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);

        // Same message for "unknown email" and "wrong password" - don't tell an attacker
        // which email addresses exist.
        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
            throw new BusinessRuleException("Invalid email or password.");

        // Administrators can lock customer accounts (Admin > Customers).
        if (await userManager.IsLockedOutAsync(user))
            throw new ForbiddenException("This account has been locked. Please contact the salon.");

        var roles = await userManager.GetRolesAsync(user);
        return tokenService.CreateToken(user, roles);
    }

    public async Task<UserProfileDto> GetProfileAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("User not found.");

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            RegistrationDate = user.RegistrationDate,
            Loyalty = await loyaltyService.GetSummaryAsync(userId)
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("User not found.");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BusinessRuleException(string.Join(" ", result.Errors.Select(e => e.Description)));

        return await GetProfileAsync(userId);
    }
}
