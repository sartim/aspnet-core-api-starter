using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AspNetCoreApiStarter.Authorization;
using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AspNetCoreApiStarter.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AuthSecurityOptions _security = AuthSecurityOptions.FromEnvironment();

    public AuthController(ApplicationDbContext context) => _context = context;

    [HttpPost("generate-jwt")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateToken(Login login)
    {
        var user = await FindUser(login.Email);
        var now = DateTime.UtcNow;
        if (user is null || user.LockoutEnd > now)
            return UnauthorizedResponse();

        if (!VerifyPassword(login.Password, user.Password))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _security.MaxFailedLoginAttempts)
            {
                user.LockoutEnd = now.AddMinutes(_security.LockoutMinutes);
                user.FailedLoginAttempts = 0;
            }
            await _context.SaveChangesAsync();
            return UnauthorizedResponse();
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = now;
        var result = await IssueTokens(user, now);
        await _context.SaveChangesAsync();
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var hash = HashToken(request.RefreshToken);
        var stored = await _context.RefreshTokens.Include(token => token.User)
            .ThenInclude(user => user.UserRoles).ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions).ThenInclude(rolePermission => rolePermission.Permission)
            .SingleOrDefaultAsync(token => token.TokenHash == hash);
        var now = DateTime.UtcNow;
        if (stored is null || stored.RevokedAt.HasValue || stored.ExpiresAt <= now || !stored.User.IsActive)
            return UnauthorizedResponse();

        stored.RevokedAt = now;
        var result = await IssueTokens(stored.User, now);
        stored.ReplacedByTokenHash = HashToken(result.RefreshToken);
        await _context.SaveChangesAsync();
        return Ok(result);
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke()
    {
        var jwtId = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (!string.IsNullOrWhiteSpace(jwtId) &&
            long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Exp), out var unixSeconds))
        {
            _context.RevokedAccessTokens.Add(new RevokedAccessToken
            {
                JwtId = jwtId,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime,
                RevokedAt = DateTime.UtcNow
            });
        }

        var email = User.FindFirstValue(ClaimTypes.Email);
        if (email is not null)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(token => token.User.Email == email && token.RevokedAt == null)
                .ToListAsync();
            foreach (var token in activeTokens)
                token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<User?> FindUser(string email) =>
        await _context.Users.Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions).ThenInclude(rolePermission => rolePermission.Permission)
            .SingleOrDefaultAsync(user => user.Email == email && user.IsActive);

    private async Task<TokenResponse> IssueTokens(User user, DateTime now)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var accessToken = CreateAccessToken(user, now);
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id, User = user, TokenHash = HashToken(refreshToken), CreatedAt = now,
            ExpiresAt = now.AddSeconds(_security.RefreshTokenLifetimeSeconds)
        });
        return new TokenResponse(accessToken.Token, refreshToken, accessToken.ExpiresAt);
    }

    private AccessToken CreateAccessToken(User user, DateTime now)
    {
        var expiresAt = now.AddSeconds(_security.AccessTokenLifetimeSeconds);
        var claims = new List<Claim> { new(ClaimTypes.Email, user.Email), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")) };
        claims.AddRange(user.UserRoles.Select(userRole => new Claim(ClaimTypes.Role, userRole.Role.Name)));
        claims.AddRange(user.UserRoles.SelectMany(userRole => userRole.Role.RolePermissions)
            .Select(link => new Claim(AuthorizationPolicies.PermissionClaimType, link.Permission.Name)));
        var token = new JwtSecurityToken(
            issuer: Environment.GetEnvironmentVariable("JWT_ISSUER"), audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            claims: claims, notBefore: now, expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")!)),
                SecurityAlgorithms.HmacSha256));
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool VerifyPassword(string password, string passwordHash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, passwordHash); }
        catch { return false; }
    }

    private ObjectResult UnauthorizedResponse() => new(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized, Title = "Unauthorized",
        Detail = "The supplied credentials are invalid or the account is temporarily unavailable.",
        Instance = HttpContext.Request.Path,
        Extensions = { ["traceId"] = StarterProblemDetails.GetTraceId(HttpContext) }
    }) { StatusCode = StatusCodes.Status401Unauthorized, ContentTypes = { "application/problem+json" } };

    private sealed record AccessToken(string Token, DateTime ExpiresAt);
    private sealed record TokenResponse(string Token, string RefreshToken, DateTime ExpiresAt);
}
