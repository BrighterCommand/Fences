namespace Paramore.Fences.Core.Benchmarks.Utils;

internal static partial class Helper
{
    public static object CreateRetry(FencesVersion technology)
    {
        var delay = TimeSpan.FromSeconds(10);

        return technology switch
        {
            FencesVersion.V7 =>
                Policy
                    .HandleResult(Failure)
                    .Or<InvalidOperationException>()
                    .WaitAndRetryAsync(3, attempt => delay, (_, _) => Task.CompletedTask),

            FencesVersion.V8 => CreateStrategy(builder =>
            {
                builder.AddRetry(new RetryStrategyOptions<string>
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Constant,
                    Delay = delay,
                    ShouldHandle = args => args.Outcome switch
                    {
                        { Exception: InvalidOperationException } => PredicateResult.True(),
                        { Result: string result } when result == Failure => PredicateResult.True(),
                        _ => PredicateResult.False(),
                    },
                    OnRetry = _ => default,
                });
            }),
            _ => throw new NotSupportedException(),
        };
    }
}
