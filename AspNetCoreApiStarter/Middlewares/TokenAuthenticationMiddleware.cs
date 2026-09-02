using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreApiStarter.Observability;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            string token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var principal = ValidateToken(token);
            bool isTokenValid = principal is not null;
            if (isTokenValid)
            {
                var jwtId = principal!.FindFirstValue(JwtRegisteredClaimNames.Jti);
                isTokenValid = string.IsNullOrWhiteSpace(jwtId) ||
                    !await dbContext.RevokedAccessTokens.AnyAsync(revoked => revoked.JwtId == jwtId && revoked.ExpiresAt > DateTime.UtcNow);
            }

            if (!isTokenValid)
            {
                await StarterProblemDetails.WriteAsync(context, StatusCodes.Status401Unauthorized,
                    "Unauthorized", "The access token is invalid or expired.");
                return;
            }
        }
        else
        {
            await StarterProblemDetails.WriteAsync(context, StatusCodes.Status401Unauthorized,
                "Unauthorized", "Provide a valid Bearer token in the Authorization header.");
            return;
        }

        await _next(context);
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                Environment.GetEnvironmentVariable("JWT_SECRET_KEY"))),
            ValidateIssuer = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidateAudience = true,
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(
                token, validationParameters, out SecurityToken validatedToken);
            return claimsPrincipal;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
