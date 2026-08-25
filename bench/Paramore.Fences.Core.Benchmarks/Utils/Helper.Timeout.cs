namespace Paramore.Fences.Core.Benchmarks.Utils;

internal static partial class Helper
{
    public static object CreateTimeout(FencesVersion technology)
    {
        var timeout = TimeSpan.FromSeconds(10);

        return technology switch
        {
            FencesVersion.V7 => Policy.TimeoutAsync<string>(timeout),
            FencesVersion.V8 => CreateStrategy(builder => builder.AddTimeout(timeout)),
            _ => throw new NotSupportedException()
        };
    }
}
