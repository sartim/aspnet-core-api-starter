using System.Security.Cryptography;
using System.Text;
using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

namespace AspNetCoreApiStarter.Email;

public enum EmailActionPurpose
{
    PasswordReset,
    EmailVerification
}

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string actionUrl, CancellationToken cancellationToken = default);
}

public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string recipient, string subject, string actionUrl, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email action requested for {Recipient}; configure an IEmailSender implementation to deliver it", recipient);
        return Task.CompletedTask;
    }
}

public sealed class EmailActionService(
    ApplicationDbContext db,
    IEmailSender emailSender,
    IConfiguration configuration)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task RequestAsync(string email, EmailActionPurpose purpose, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email && candidate.IsActive, cancellationToken);
        if (user is null)
            return;

        var purposeValue = purpose.ToString();
        var now = DateTime.UtcNow;
        var previous = await db.EmailActionTokens
            .Where(token => token.UserId == user.Id && token.Purpose == purposeValue && token.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in previous)
            token.UsedAt = now;

        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        db.EmailActionTokens.Add(new EmailActionToken
        {
            UserId = user.Id,
            Purpose = purposeValue,
            TokenHash = Hash(rawToken),
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now.Add(TokenLifetime)
        });
        await db.SaveChangesAsync(cancellationToken);

        var baseUrl = configuration["EMAIL_ACTION_BASE_URL"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            var action = purpose == EmailActionPurpose.PasswordReset ? "reset-password" : "verify-email";
            var actionUrl = $"{baseUrl.TrimEnd('/')}/{action}?token={Uri.EscapeDataString(rawToken)}";
            await emailSender.SendAsync(user.Email, $"{action} requested", actionUrl, cancellationToken);
        }
    }

    public async Task<User?> ConsumeAsync(string rawToken, EmailActionPurpose purpose, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var token = await db.EmailActionTokens.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == Hash(rawToken) && item.Purpose == purpose.ToString() &&
                item.UsedAt == null && item.ExpiresAt > now, cancellationToken);
        if (token is null)
            return null;

        token.UsedAt = now;
        token.UpdatedAt = now;
        return token.User;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken);

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
