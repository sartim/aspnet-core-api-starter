using AspNetCoreApiStarter.Email;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;

namespace AspNetCoreApiStarter.Tests.Email;

public class EmailActionServiceTests
{
    [Fact]
    public async Task PasswordResetToken_IsHashedAndSingleUse()
    {
        await using var db = DbContextHelper.GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = 1,
            Password = "old-password-hash",
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sender = new RecordingEmailSender();
        var configuration = new ConfigurationManager
        {
            ["EMAIL_ACTION_BASE_URL"] = "https://app.example.com/account"
        };
        var service = new EmailActionService(db, sender, configuration);

        await service.RequestAsync(user.Email, EmailActionPurpose.PasswordReset);

        var stored = Assert.Single(db.EmailActionTokens);
        Assert.DoesNotContain(sender.ActionUrl!, stored.TokenHash, StringComparison.Ordinal);
        var token = sender.ActionUrl!.Split("token=", StringSplitOptions.None)[1];
        var consumed = await service.ConsumeAsync(token, EmailActionPurpose.PasswordReset);
        await service.SaveAsync();

        Assert.Equal(user.Id, consumed?.Id);
        Assert.Null(await service.ConsumeAsync(token, EmailActionPurpose.PasswordReset));
        Assert.NotNull(stored.UsedAt);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public string? ActionUrl { get; private set; }

        public Task SendAsync(string recipient, string subject, string actionUrl, CancellationToken cancellationToken = default)
        {
            ActionUrl = actionUrl;
            return Task.CompletedTask;
        }
    }
}
