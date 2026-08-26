#pragma warning disable S4225 // Extension methods should not extend "object"

namespace Paramore.Fences.Core.Benchmarks.Utils;

internal static partial class Helper
{
    public static async ValueTask ExecuteAsync(this object obj, FencesVersion version)
    {
        switch (version)
        {
            case FencesVersion.V7:
                await ((IAsyncPolicy<string>)obj).ExecuteAsync(static _ => Task.FromResult("dummy"), CancellationToken.None).ConfigureAwait(false);
                return;
            case FencesVersion.V8:
                var context = ResilienceContextPool.Shared.Get();

                await ((ResiliencePipeline<string>)obj).ExecuteOutcomeAsync(
                    static (_, _) => Outcome.FromResultAsValueTask("dummy"),
                    context,
                    string.Empty).ConfigureAwait(false);

                ResilienceContextPool.Shared.Return(context);
                return;
        }

        throw new NotSupportedException();
    }

    private static ResiliencePipeline<string> CreateStrategy(Action<ResiliencePipelineBuilder<string>> configure)
    {
        var builder = new ResiliencePipelineBuilder<string>();
        configure(builder);
        return builder.Build();
    }
}
