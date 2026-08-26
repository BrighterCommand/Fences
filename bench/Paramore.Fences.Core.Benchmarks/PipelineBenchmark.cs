namespace Paramore.Fences.Core.Benchmarks;

public class PipelineBenchmark
{
    private object? _pipelineV7;
    private object? _pipelineV8;

    [GlobalSetup]
    public void Setup()
    {
        _pipelineV7 = Helper.CreatePipeline(FencesVersion.V7, Components);
        _pipelineV8 = Helper.CreatePipeline(FencesVersion.V8, Components);
    }

    [Params(1, 2, 5, 10)]
    public int Components { get; set; }

    [Benchmark(Baseline = true)]
    public ValueTask ExecutePipeline_V7() => _pipelineV7!.ExecuteAsync(FencesVersion.V7);

    [Benchmark]
    public ValueTask ExecutePipeline_V8() => _pipelineV8!.ExecuteAsync(FencesVersion.V8);
}
