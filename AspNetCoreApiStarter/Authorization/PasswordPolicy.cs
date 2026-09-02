namespace AspNetCoreApiStarter.Authorization;

public static class PasswordPolicy
{
    public const int DefaultMinimumLength = 12;

    public static bool IsValid(string? password, int minimumLength = DefaultMinimumLength)
    {
        if (string.IsNullOrEmpty(password) || password.Length < minimumLength)
            return false;

        return password.Any(char.IsUpper) &&
            password.Any(char.IsLower) &&
            password.Any(char.IsDigit) &&
            password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    public static string Requirements(int minimumLength = DefaultMinimumLength) =>
        $"Password must be at least {minimumLength} characters and include uppercase, lowercase, numeric, and special characters.";
}
