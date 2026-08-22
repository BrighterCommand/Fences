namespace Paramore.Fences.Core.Tests.Utils;

public class TypeNameFormatterTests
{
    [Fact]
    public void AsString_Ok()
    {
        Paramore.Fences.Utils.TypeNameFormatter.Format(typeof(string)).ShouldBe("String");
        Paramore.Fences.Utils.TypeNameFormatter.Format(typeof(List<string>)).ShouldBe("List<String>");
        Paramore.Fences.Utils.TypeNameFormatter.Format(typeof(KeyValuePair<string, string>)).ShouldBe("KeyValuePair`2");
    }
}
