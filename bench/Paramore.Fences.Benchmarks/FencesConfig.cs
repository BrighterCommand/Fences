namespace Paramore.Fences.Benchmarks;

internal class FencesConfig : ManualConfig
{
    public FencesConfig()
    {
        AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
        AddJob(Job.Default);
    }
}
