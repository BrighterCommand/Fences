// This sample drives Microsoft.Extensions.Http.Resilience, which is built on
// Polly, so the ResilienceContext handed to these callbacks is Polly's.
using Polly;

namespace Chaos;

/// <summary>
/// Abstraction for controlling chaos injection.
/// </summary>
public interface IChaosManager
{
    ValueTask<bool> IsChaosEnabledAsync(ResilienceContext context);

    ValueTask<double> GetInjectionRateAsync(ResilienceContext context);
}