using Paramore.Fences.Timeout;

namespace Paramore.Fences.Core.Tests.Timeout;

public class TimeoutConstantsTests
{
    [Fact]
    public void EnsureDefaultValues() =>
        TimeoutConstants.OnTimeoutEvent.ShouldBe("OnTimeout");
}
