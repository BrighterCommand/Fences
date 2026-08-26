namespace Paramore.Fences.Core.Benchmarks;

public class RateLimiterBenchmark
{
    private object? _rateLimiterV7;
    private object? _rateLimiterV8;

    [GlobalSetup]
    public void Setup()
    {
        _rateLimiterV7 = Helper.CreateRateLimiter(FencesVersion.V7);
        _rateLimiterV8 = Helper.CreateRateLimiter(FencesVersion.V8);
    }

    [Benchmark(Baseline = true)]
    public ValueTask ExecuteRateLimiter_V7() => _rateLimiterV7!.ExecuteAsync(FencesVersion.V7);

    [Benchmark]
    public ValueTask ExecuteRateLimiter_V8() => _rateLimiterV8!.ExecuteAsync(FencesVersion.V8);
}
