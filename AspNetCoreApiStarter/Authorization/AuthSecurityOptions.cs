namespace AspNetCoreApiStarter.Authorization;

public sealed record AuthSecurityOptions(
    int AccessTokenLifetimeSeconds,
    int RefreshTokenLifetimeSeconds,
    int MaxFailedLoginAttempts,
    int LockoutMinutes)
{
    public static AuthSecurityOptions FromEnvironment()
    {
        return new(
            GetPositiveInt("JWT_EXPIRY", 300),
            GetPositiveInt("JWT_REFRESH_EXPIRY", 604800),
            GetPositiveInt("AUTH_MAX_FAILED_ATTEMPTS", 5),
            GetPositiveInt("AUTH_LOCKOUT_MINUTES", 15));
    }

    private static int GetPositiveInt(string name, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
    }
}
