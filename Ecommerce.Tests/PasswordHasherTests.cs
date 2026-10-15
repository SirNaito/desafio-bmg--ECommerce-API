using Ecommerce.Api.Services;

namespace Ecommerce.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_UsesRandomSaltAndVerifiesPassword()
    {
        var first = PasswordHasher.Hash("test-password");
        var second = PasswordHasher.Hash("test-password");
        Assert.NotEqual(first, second);
        Assert.True(PasswordHasher.Verify("test-password", first));
        Assert.True(PasswordHasher.Verify("test-password", second));
    }

    [Theory]
    [InlineData("wrong-password")]
    [InlineData("")]
    public void Verify_RejectsIncorrectPassword(string password) =>
        Assert.False(PasswordHasher.Verify(password, PasswordHasher.Hash("expected-password")));

    [Fact]
    public void Verify_RejectsMalformedHash() =>
        Assert.False(PasswordHasher.Verify("password", "invalid-hash"));
}
