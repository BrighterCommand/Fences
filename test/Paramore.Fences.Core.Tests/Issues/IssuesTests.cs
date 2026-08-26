using Microsoft.Extensions.Time.Testing;

namespace Paramore.Fences.Core.Tests.Issues;

public partial class IssuesTests
{
    private FakeTimeProvider TimeProvider { get; } = new FakeTimeProvider();
}
