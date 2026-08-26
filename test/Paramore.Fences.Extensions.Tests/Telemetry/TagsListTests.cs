using Paramore.Fences.Telemetry;

namespace Paramore.Fences.Extensions.Tests.Telemetry;

[Collection(nameof(NonParallelizableCollection))]
public class TagsListTests
{
    [Fact]
    public async Task Pooling_OK() =>
        await TestUtilities.AssertWithTimeoutAsync(() =>
        {
            var context = TagsList.Get();

            TagsList.Return(context);

            TagsList.Get().ShouldBeSameAs(context);
        });
}
