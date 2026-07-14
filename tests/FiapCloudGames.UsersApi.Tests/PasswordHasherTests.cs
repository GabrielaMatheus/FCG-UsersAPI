using FiapCloudGames.UsersApi.Services;
using Xunit;

namespace FiapCloudGames.UsersApi.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashAndVerify_WithValidPassword_ReturnsTrue()
    {
        var sut = new PasswordHasher();
        var hash = sut.Hash("Strong@123");
        Assert.True(sut.Verify("Strong@123", hash));
        Assert.False(sut.Verify("Wrong@123", hash));
        Assert.DoesNotContain("Strong@123", hash);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("onlyletters")]
    [InlineData("Letters123")]
    public void Hash_WithWeakPassword_Throws(string password) =>
        Assert.Throws<ArgumentException>(() => new PasswordHasher().Hash(password));
}
