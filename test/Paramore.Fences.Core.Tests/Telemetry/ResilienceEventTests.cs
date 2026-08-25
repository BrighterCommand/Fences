using Paramore.Fences.Telemetry;

namespace Paramore.Fences.Core.Tests.Telemetry;

public class ResilienceEventTests
{
    [Fact]
    public void Ctor_Ok()
    {
        var ev = new ResilienceEvent(ResilienceEventSeverity.Warning, "A");

        ev.ToString().ShouldBe("A");
        ev.Severity.ShouldBe(ResilienceEventSeverity.Warning);
    }
}
