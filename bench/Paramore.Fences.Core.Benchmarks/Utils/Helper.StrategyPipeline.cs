namespace Paramore.Fences.Core.Benchmarks.Utils;

internal static partial class Helper
{
    public static object CreatePipeline(FencesVersion technology, int count) => technology switch
    {
        FencesVersion.V7 => count == 1 ? Policy.NoOpAsync<string>() : Policy.WrapAsync([.. Enumerable.Repeat(0, count).Select(_ => Policy.NoOpAsync<string>())]),

        FencesVersion.V8 => CreateStrategy(builder =>
        {
            for (var i = 0; i < count; i++)
            {
                builder.AddStrategy(static _ => new EmptyResilienceStrategy(), new EmptyResilienceOptions());
            }
        }),
        _ => throw new NotSupportedException()
    };
}
