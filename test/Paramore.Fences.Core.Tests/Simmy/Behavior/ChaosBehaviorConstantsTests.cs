using Paramore.Fences.Simmy.Behavior;

namespace Paramore.Fences.Core.Tests.Simmy.Behavior;

public class ChaosBehaviorConstantsTests
{
    [Fact]
    public void EnsureDefaults()
    {
        ChaosBehaviorConstants.DefaultName.ShouldBe("Chaos.Behavior");
        ChaosBehaviorConstants.OnBehaviorInjectedEvent.ShouldBe("Chaos.OnBehavior");
    }
}
