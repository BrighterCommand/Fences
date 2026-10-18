using Xunit;

namespace Paramore.Fences;

internal static class TestCancellation
{
    public static CancellationToken Token => TestContext.Current.CancellationToken;
}
