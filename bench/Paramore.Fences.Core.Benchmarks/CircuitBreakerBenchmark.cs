namespace Paramore.Fences.Core.Benchmarks;

public class CircuitBreakerBenchmark
{
    private object? _circuitBreakerV7;
    private object? _circuitBreakerV8;

    [GlobalSetup]
    public void Setup()
    {
        _circuitBreakerV7 = Helper.CreateCircuitBreaker(FencesVersion.V7);
        _circuitBreakerV8 = Helper.CreateCircuitBreaker(FencesVersion.V8);
    }

    [Benchmark(Baseline = true)]
    public ValueTask ExecuteCircuitBreaker_V7() => _circuitBreakerV7!.ExecuteAsync(FencesVersion.V7);

    [Benchmark]
    public ValueTask ExecuteCircuitBreaker_V8() => _circuitBreakerV8!.ExecuteAsync(FencesVersion.V8);
}
