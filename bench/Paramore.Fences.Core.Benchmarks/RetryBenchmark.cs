namespace Paramore.Fences.Core.Benchmarks;

public class RetryBenchmark
{
    private object? _retryV7;
    private object? _retryV8;

    [GlobalSetup]
    public void Setup()
    {
        _retryV7 = Helper.CreateRetry(FencesVersion.V7);
        _retryV8 = Helper.CreateRetry(FencesVersion.V8);
    }

    [Benchmark(Baseline = true)]
    public ValueTask ExecuteRetry_V7() => _retryV7!.ExecuteAsync(FencesVersion.V7);

    [Benchmark]
    public ValueTask ExecuteRetry_V8() => _retryV8!.ExecuteAsync(FencesVersion.V8);
}
