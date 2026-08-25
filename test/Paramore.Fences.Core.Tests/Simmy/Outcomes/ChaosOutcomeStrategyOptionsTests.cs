using Paramore.Fences.Simmy;
using Paramore.Fences.Simmy.Outcomes;

namespace Paramore.Fences.Core.Tests.Simmy.Outcomes;

public class ChaosOutcomeStrategyOptionsTests
{
    [Fact]
    public void Ctor_Ok()
    {
        var sut = new ChaosOutcomeStrategyOptions<int>();
        sut.Randomizer.ShouldNotBeNull();
        sut.Enabled.ShouldBeTrue();
        sut.EnabledGenerator.ShouldBeNull();
        sut.InjectionRate.ShouldBe(ChaosStrategyConstants.DefaultInjectionRate);
        sut.InjectionRateGenerator.ShouldBeNull();
        sut.OnOutcomeInjected.ShouldBeNull();
        sut.OutcomeGenerator.ShouldBeNull();
    }
}
