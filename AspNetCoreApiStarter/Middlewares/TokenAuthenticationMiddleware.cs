using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreApiStarter.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            string token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            bool isTokenValid = ValidateToken(token);

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

    private bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                Environment.GetEnvironmentVariable("JWT_SECRET_KEY"))),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        try
        {
            ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(
                token, validationParameters, out SecurityToken validatedToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
