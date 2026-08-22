using System.Threading.Tasks;
using Paramore.Fences.Utils.Pipeline;

namespace Paramore.Fences.Core.Tests.Utils.Pipeline;

public class PipelineComponentTests
{
    [Fact]
    public async Task Dispose_Ok()
    {
        PipelineComponent.Empty.ShouldNotBeNull();
        await PipelineComponent.Empty.DisposeAsync();
    }
}
