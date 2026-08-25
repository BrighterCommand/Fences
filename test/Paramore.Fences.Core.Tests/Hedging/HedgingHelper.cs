using Paramore.Fences.Hedging;
using Paramore.Fences.Hedging.Utils;

namespace Paramore.Fences.Core.Tests.Hedging;

internal static class HedgingHelper
{
    public static HedgingHandler<T> CreateHandler<T>(
        Func<Outcome<T>, bool> shouldHandle,
        Func<HedgingActionGeneratorArguments<T>, Func<ValueTask<Outcome<T>>>?> generator,
        Func<OnHedgingArguments<T>, ValueTask>? onHedging = null) =>
        new(args => new ValueTask<bool>(shouldHandle(args.Outcome!))!, generator, onHedging);
}

