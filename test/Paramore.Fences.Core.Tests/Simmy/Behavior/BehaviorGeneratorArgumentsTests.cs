using Paramore.Fences.Simmy.Behavior;

namespace Paramore.Fences.Core.Tests.Simmy.Behavior;

public static class BehaviorGeneratorArgumentsTests
{
    [Fact]
    public static void Ctor_Ok()
    {
        // Arrange
        var context = ResilienceContextPool.Shared.Get(TestCancellation.Token);

        // Act
        var args = new BehaviorGeneratorArguments(context);

        // Assert
        args.Context.ShouldBe(context);
    }
}
