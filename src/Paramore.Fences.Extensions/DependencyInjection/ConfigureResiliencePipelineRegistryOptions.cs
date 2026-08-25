using Paramore.Fences.Registry;

namespace Paramore.Fences.DependencyInjection;

internal sealed class ConfigureResiliencePipelineRegistryOptions<TKey>
    where TKey : notnull
{
    public List<Action<ResiliencePipelineRegistry<TKey>>> Actions { get; } = [];
}
