using AspNetCoreApiStarter.Authorization;

namespace AspNetCoreApiStarter.Tests.Authorization;

public class PasswordPolicyTests
{
    [Fact]
    public void IsValid_RequiresLengthAndCharacterDiversity()
    {
        Assert.False(PasswordPolicy.IsValid("short"));
        Assert.False(PasswordPolicy.IsValid("longpasswordonly"));
        Assert.True(PasswordPolicy.IsValid("Strong-password1"));
    }
}
