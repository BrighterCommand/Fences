using Paramore.Fences.Telemetry;

namespace Paramore.Fences.Extensions.Tests.Telemetry;

public static class TelemetrySourceTests
{
    [Fact]
    public static void TelemetrySource_CreatesMeter()
    {
        var source = TelemetrySource.Instance;

        source.ShouldNotBeNull();

        source.Meter.ShouldNotBeNull();
        source.Meter.Name.ShouldBe("Paramore.Fences");
        source.Meter.Version.ShouldNotBeNullOrEmpty();
        source.Meter.Version.ShouldNotContain('-');
        source.Meter.Version.ShouldNotContain('+');
        Version.TryParse(source.Meter.Version, out var version).ShouldBeTrue();
        version.ShouldBeGreaterThan(new(0, 0, 0));
    }
}
