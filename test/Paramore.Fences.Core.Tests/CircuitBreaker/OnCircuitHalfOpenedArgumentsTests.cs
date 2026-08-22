using Paramore.Fences.CircuitBreaker;

namespace Paramore.Fences.Core.Tests.CircuitBreaker;

public static class OnCircuitHalfOpenedArgumentsTests
{
    [Fact]
    public static void Ctor_Ok()
    {
        // Arrange
        var context = ResilienceContextPool.Shared.Get(TestCancellation.Token);

        // Act
        var target = new OnCircuitHalfOpenedArguments(context);

        // Assert
        target.Context.ShouldBe(context);
    }
}
