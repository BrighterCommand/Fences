using System.ComponentModel.DataAnnotations;
using Paramore.Fences.Simmy;
using Paramore.Fences.Simmy.Fault;
using Paramore.Fences.Utils;

namespace Paramore.Fences.Core.Tests.Simmy.Fault;

public class ChaosFaultStrategyOptionsTests
{
    [Fact]
    public void Ctor_Ok()
    {
        var sut = new ChaosFaultStrategyOptions();
        sut.Randomizer.ShouldNotBeNull();
        sut.Enabled.ShouldBeTrue();
        sut.EnabledGenerator.ShouldBeNull();
        sut.InjectionRate.ShouldBe(ChaosStrategyConstants.DefaultInjectionRate);
        sut.InjectionRateGenerator.ShouldBeNull();
        sut.OnFaultInjected.ShouldBeNull();
        sut.FaultGenerator.ShouldBeNull();
    }

    [Fact]
    public void InvalidOptions()
    {
        var options = new ChaosFaultStrategyOptions
        {
            FaultGenerator = null!,
        };

        var exception = Should.Throw<ValidationException>(() => ValidationHelper.ValidateObject(new(options, "Invalid Options")));
        exception.Message.Trim().ShouldBe("""
            Invalid Options
            Validation Errors:
            The FaultGenerator field is required.
            """,
            StringCompareShould.IgnoreLineEndings);
    }
}
