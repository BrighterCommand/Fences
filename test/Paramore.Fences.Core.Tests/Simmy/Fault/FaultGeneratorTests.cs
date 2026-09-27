using Paramore.Fences.Simmy.Fault;

namespace Paramore.Fences.Core.Tests.Simmy.Fault;

public class FaultGeneratorTests
{
    [Fact]
    public void AddException_Generic_Ok()
    {
        var generator = new FaultGenerator();

        generator.AddException<InvalidOperationException>();

        Generate(generator).ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public void AddException_Factory_Ok()
    {
        var generator = new FaultGenerator();

        generator.AddException(() => new InvalidOperationException());

        Generate(generator).ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public void AddException_FactoryWithResilienceContext_Ok()
    {
        var generator = new FaultGenerator();

        generator.AddException(context =>
        {
            context.ShouldNotBeNull();

            return new InvalidOperationException();
        });

        Generate(generator).ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_NoExceptionRegistered_ReturnsNull()
    {
        // Arrange
        var generator = new FaultGenerator();

        // Act
        var fault = Generate(generator);

        // Assert
        fault.ShouldBeNull();
    }

    [Fact]
    public void ImplicitConversion_AllWeightsZero_ReturnsNull()
    {
        // Arrange
        var generator = new FaultGenerator();
        generator.AddException<InvalidOperationException>(weight: 0);

        // Act
        var fault = Generate(generator);

        // Assert
        fault.ShouldBeNull();
    }

    private static Exception? Generate(FaultGenerator generator)
    {
        Func<FaultGeneratorArguments, ValueTask<Exception?>> func = generator;

        return func(
            new FaultGeneratorArguments(
                ResilienceContextPool.Shared.Get(TestCancellation.Token))).AsTask().Result;
    }
}
