namespace Paramore.Fences.Tests;

// The tests can only be strong named if they only depend on assemblies that are strong named,
// therefore if the tests are strong named then the libraries we ship are strong named.

public static class StrongNameTests
{
    [Fact]
    public static void Tests_Are_Strong_Named()
    {
        // Arrange
        var assembly = typeof(StrongNameTests).Assembly;
        var name = assembly.GetName();

        // Act
        var actual = name.GetPublicKey();

        // Assert
        Assert.NotNull(actual);
        Assert.NotEmpty(actual);

        // Split across lines to stay inside the 500-character line limit (S103):
        // the 2048-bit key is 576 hex characters, where Polly's 1024-bit key was 320.
        Assert.Equal(
            "0024000004800000140100000602000000240000525341310008000001000100AB8C96F5FC6E22E0A4611B0580A11A37"
            + "7A76AAA6B894EE8AE576FD4B866F287AA9800C7B87098A438846BA28812C5F3924FD246E9B293A21A9D426B1077E7186"
            + "65B86CB3CA3B0AC237D86685CA3AE82CEEA4908C2491D440C4DBCF655FFA1E3B5AF3DEB49DE66BCD125057D1D09674EA"
            + "38BCFE6FABB2B9C1FE3F28D285F36E0201E305B3CA0938B484A2A9450445F93E83D32DAD0CF8DB815C092103C36C78C2"
            + "98028E8733F05CC78F87B2931274ED37A9DEFAD74C0738FA18D104994E304EA722891BA8B1D95CD5FB48683E4077BA77"
            + "ECD101FFFC784CFF6316D722B1503562BCB6FEDC853CC4962F5C2F92C28B23DE5CF4852BC6E08DFA3AF67F747BF1DFC5",
            ToHexString(actual));

        // Act
        actual = name.GetPublicKeyToken();

        // Assert
        Assert.NotNull(actual);
        Assert.NotEmpty(actual);

        Assert.Equal("6998A40D28482B6D", ToHexString(actual));
    }

    private static string ToHexString(byte[] bytes)
    {
#if NET
        return Convert.ToHexString(bytes);
#else
        var builder = new System.Text.StringBuilder(bytes.Length * 2);

        foreach (var b in bytes)
        {
            builder.Append(b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
        }

        return builder.ToString();
#endif
    }
}
