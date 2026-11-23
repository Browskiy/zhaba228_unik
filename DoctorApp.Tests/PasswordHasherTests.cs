using DoctorApp.Services;

namespace DoctorApp.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        string hash = PasswordHasher.Hash("secret123");

        Assert.True(PasswordHasher.Verify("secret123", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        string hash = PasswordHasher.Hash("secret123");

        Assert.False(PasswordHasher.Verify("other", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        Assert.NotEqual(PasswordHasher.Hash("secret123"), PasswordHasher.Hash("secret123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("100000.@@@.@@@")]
    [InlineData("0.AAAA.AAAA")]
    public void Verify_CorruptedHash_ReturnsFalseWithoutException(string hash)
    {
        Assert.False(PasswordHasher.Verify("secret123", hash));
    }
}
