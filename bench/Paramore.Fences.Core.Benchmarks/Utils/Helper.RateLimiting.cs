using System.Threading.RateLimiting;

namespace Paramore.Fences.Core.Benchmarks.Utils;

internal static partial class Helper
{
    public static object CreateRateLimiter(FencesVersion technology)
    {
        var timeout = TimeSpan.FromSeconds(10);

        return technology switch
        {
            FencesVersion.V7 => Policy.BulkheadAsync<string>(10, 10),
            FencesVersion.V8 => CreateStrategy(builder => builder.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 10,
                QueueLimit = 10
            })),
            _ => throw new NotSupportedException()
        };
    }
}
