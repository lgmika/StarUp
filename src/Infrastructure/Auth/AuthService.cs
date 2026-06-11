using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Auth.Dtos;
using StartupConnect.Application.Auth.Interfaces;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Auth;

public sealed class AuthService(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    SecureTokenGenerator tokenGenerator,
    JwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        ValidateRegister(request);

        var normalizedEmail = NormalizeEmail(request.Email);
        var emailExists = await dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new ApiException("Email is already registered", HttpStatusCode.Conflict);
        }

        var userRole = await GetRoleAsync(SystemRoles.User, cancellationToken);
        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            FullName = request.FullName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        user.UserRoles.Add(new UserRole { User = user, Role = userRole });
        dbContext.Users.Add(user);

        var verificationToken = tokenGenerator.CreateToken();
        dbContext.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            User = user,
            TokenHash = tokenGenerator.HashToken(verificationToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        });

        AddAudit(user.Id, "Auth.Register", "User", user.Id, ipAddress, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, ipAddress, cancellationToken, verificationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        ValidateEmail(request.Email);
        ValidateRequired(request.Password, "password", "Password is required");

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await GetUserWithRolesAsync(normalizedEmail, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ApiException("Invalid email or password", HttpStatusCode.Unauthorized);
        }

        if (user.IsSuspended)
        {
            throw new ApiException("User account is suspended", HttpStatusCode.Forbidden);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(user.Id, "Auth.Login", "User", user.Id, ipAddress, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        ValidateRequired(request.RefreshToken, "refreshToken", "Refresh token is required");

        var tokenHash = tokenGenerator.HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            throw new ApiException("Invalid refresh token", HttpStatusCode.Unauthorized);
        }

        if (refreshToken.User.IsSuspended)
        {
            throw new ApiException("User account is suspended", HttpStatusCode.Forbidden);
        }

        var replacementToken = tokenGenerator.CreateToken();
        var replacementHash = tokenGenerator.HashToken(replacementToken);

        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReplacedByTokenHash = replacementHash;

        var replacement = CreateRefreshToken(refreshToken.User, replacementHash, ipAddress);
        dbContext.RefreshTokens.Add(replacement);
        AddAudit(refreshToken.UserId, "Auth.RefreshToken", "User", refreshToken.UserId, ipAddress, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(refreshToken.User, replacementToken, replacement.ExpiresAt);
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        ValidateRequired(request.RefreshToken, "refreshToken", "Refresh token is required");

        var tokenHash = tokenGenerator.HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (refreshToken is null)
        {
            return;
        }

        refreshToken.RevokedAt ??= DateTimeOffset.UtcNow;
        refreshToken.RevokedByIp ??= ipAddress;
        AddAudit(refreshToken.UserId, "Auth.Logout", "RefreshToken", refreshToken.Id, ipAddress, null);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        ValidateEmail(request.Email);
        ValidateRequired(request.Token, "token", "Verification token is required");

        var normalizedEmail = NormalizeEmail(request.Email);
        var tokenHash = tokenGenerator.HashToken(request.Token);

        var token = await dbContext.EmailVerificationTokens
            .Include(emailToken => emailToken.User)
            .ThenInclude(user => user.UserRoles)
            .FirstOrDefaultAsync(emailToken =>
                emailToken.User.NormalizedEmail == normalizedEmail &&
                emailToken.TokenHash == tokenHash,
                cancellationToken);

        if (token is null || token.UsedAt is not null || token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ApiException("Invalid or expired verification token", HttpStatusCode.BadRequest);
        }

        var verifiedRole = await GetRoleAsync(SystemRoles.VerifiedUser, cancellationToken);
        token.UsedAt = DateTimeOffset.UtcNow;
        token.User.IsEmailVerified = true;
        token.User.UpdatedAt = DateTimeOffset.UtcNow;

        var hasVerifiedRole = await dbContext.UserRoles.AnyAsync(
            userRole => userRole.UserId == token.UserId && userRole.RoleId == verifiedRole.Id,
            cancellationToken);

        if (!hasVerifiedRole)
        {
            dbContext.UserRoles.Add(new UserRole { UserId = token.UserId, RoleId = verifiedRole.Id });
        }

        AddAudit(token.UserId, "Auth.VerifyEmail", "User", token.UserId, null, null);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        ValidateEmail(request.Email);

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return new ForgotPasswordResponse("If the email exists, a password reset token has been generated.");
        }

        var resetToken = tokenGenerator.CreateToken();
        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenGenerator.HashToken(resetToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        AddAudit(user.Id, "Auth.ForgotPassword", "User", user.Id, null, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ForgotPasswordResponse("If the email exists, a password reset token has been generated.", resetToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        ValidateEmail(request.Email);
        ValidatePassword(request.NewPassword);
        ValidateRequired(request.Token, "token", "Password reset token is required");

        var normalizedEmail = NormalizeEmail(request.Email);
        var tokenHash = tokenGenerator.HashToken(request.Token);

        var resetToken = await dbContext.PasswordResetTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token =>
                token.User.NormalizedEmail == normalizedEmail &&
                token.TokenHash == tokenHash,
                cancellationToken);

        if (resetToken is null || resetToken.UsedAt is not null || resetToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ApiException("Invalid or expired password reset token", HttpStatusCode.BadRequest);
        }

        resetToken.UsedAt = DateTimeOffset.UtcNow;
        resetToken.User.PasswordHash = passwordHasher.Hash(request.NewPassword);
        resetToken.User.UpdatedAt = DateTimeOffset.UtcNow;

        var activeRefreshTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == resetToken.UserId && token.RevokedAt == null && token.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        }

        AddAudit(resetToken.UserId, "Auth.ResetPassword", "User", resetToken.UserId, null, null);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userIdValue =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst("nameid")?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        var user = await dbContext.Users
            .Include(item => item.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new ApiException("User not found", HttpStatusCode.NotFound);
        }

        if (user.IsSuspended)
        {
            throw new ApiException("User account is suspended", HttpStatusCode.Forbidden);
        }

        return MapUser(user);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken,
        string? devEmailVerificationToken = null)
    {
        var refreshToken = tokenGenerator.CreateToken();
        var refreshTokenHash = tokenGenerator.HashToken(refreshToken);
        var tokenEntity = CreateRefreshToken(user, refreshTokenHash, ipAddress);

        dbContext.RefreshTokens.Add(tokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, refreshToken, tokenEntity.ExpiresAt, devEmailVerificationToken);
    }

    private AuthResponse CreateAuthResponse(
        User user,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        string? devEmailVerificationToken = null)
    {
        var authUser = MapUser(user);
        var accessToken = jwtTokenService.CreateAccessToken(authUser);

        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken,
            refreshTokenExpiresAt,
            authUser,
            devEmailVerificationToken);
    }

    private RefreshToken CreateRefreshToken(User user, string tokenHash, string? ipAddress)
    {
        return new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        };
    }

    private async Task<Role> GetRoleAsync(string code, CancellationToken cancellationToken)
    {
        return await dbContext.Roles.FirstOrDefaultAsync(role => role.Code == code, cancellationToken)
            ?? throw new InvalidOperationException($"Required role '{code}' was not seeded.");
    }

    private async Task<User?> GetUserWithRolesAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    private static AuthUserDto MapUser(User user)
    {
        return new AuthUserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.IsEmailVerified,
            user.UserRoles.Select(userRole => userRole.Role.Code).Order().ToArray());
    }

    private void AddAudit(Guid? actorUserId, string action, string resourceType, Guid? resourceId, string? ipAddress, string? userAgent)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
    }

    private static void ValidateRegister(RegisterRequest request)
    {
        ValidateEmail(request.Email);
        ValidatePassword(request.Password);
        ValidateRequired(request.FullName, "fullName", "Full name is required");

        if (request.FullName.Trim().Length > 160)
        {
            throw new ValidationException([new ErrorDetail("FullNameTooLong", "Full name must be at most 160 characters", "fullName")]);
        }
    }

    private static void ValidateEmail(string email)
    {
        ValidateRequired(email, "email", "Email is required");

        try
        {
            _ = new MailAddress(email);
        }
        catch (FormatException)
        {
            throw new ValidationException([new ErrorDetail("InvalidEmail", "Email is invalid", "email")]);
        }
    }

    private static void ValidatePassword(string password)
    {
        ValidateRequired(password, "password", "Password is required");

        if (password.Length < 8)
        {
            throw new ValidationException([new ErrorDetail("WeakPassword", "Password must be at least 8 characters", "password")]);
        }
    }

    private static void ValidateRequired(string value, string field, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException([new ErrorDetail("Required", message, field)]);
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
