namespace Paramore.Fences.Core.Benchmarks;

public class TimeoutBenchmark
{
    private object? _timeoutV7;
    private object? _timeoutV8;

    [GlobalSetup]
    public void Setup()
    {
        _timeoutV7 = Helper.CreateTimeout(FencesVersion.V7);
        _timeoutV8 = Helper.CreateTimeout(FencesVersion.V8);
    }

    [Benchmark(Baseline = true)]
    public ValueTask ExecuteTimeout_V7() => _timeoutV7!.ExecuteAsync(FencesVersion.V7);

    [Benchmark]
    public ValueTask ExecuteTimeout_V8() => _timeoutV8!.ExecuteAsync(FencesVersion.V8);
}
