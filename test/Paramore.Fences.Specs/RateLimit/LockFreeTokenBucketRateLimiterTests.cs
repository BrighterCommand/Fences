namespace Paramore.Fences.Specs.RateLimit;

public class LockFreeTokenBucketRateLimiterTests : TokenBucketRateLimiterTestsBase
{
    internal override IRateLimiter GetRateLimiter(TimeSpan onePer, long bucketCapacity) =>
        new LockFreeTokenBucketRateLimiter(onePer, bucketCapacity);
}
