# Fences

Fences is a .NET resilience and transient-fault-handling library that allows developers to express resilience strategies such as Retry, Circuit Breaker, Hedging, Timeout, Rate Limiter and Fallback in a fluent and thread-safe manner.

Fences is a community fork of [Polly](https://github.com/App-vNext/Polly), maintained by [Brighter Command](https://github.com/BrighterCommand). It is not affiliated with, endorsed by, or supported by App vNext or the Polly maintainers. The fork was taken from Polly 8.7.0 to avoid the Open Source Maintainers Fee now charged for Polly; see [ADR 0002](docs/adr/0002-fork-polly-as-fences.md) for the reasoning and [`NOTICE.md`](NOTICE.md) for provenance and attribution.

## Why Fences exists

**Polly's source is BSD 3-Clause licensed, and it stays open source.** That licence lets anyone redistribute it, in source or binary form, provided the copyright notice and disclaimer are retained - which [`NOTICE.md`](NOTICE.md) and [`LICENSE`](LICENSE) do. Building and publishing our own binaries from that source is squarely within the grant.

The Open Source Maintainers Fee (OSMF) is App vNext's policy for the binaries **they** publish: a commercial consumer of those binaries may owe a fee above a revenue threshold. It governs their distribution. It does not change the licence on the source, and it does not reach binaries built by anyone else.

**That freedom is the OSMF's own argument.** The case made for charging for binaries is that it stays within open source precisely because the source remains open and anyone is free to build and publish binaries of their own. That is what separates the model from proprietary licensing, and we think the argument is sound. But it is one door, and it opens both ways: the principle that makes the fee compatible with open source is the same principle that makes Fences legitimate. We are not going around the OSMF - we are walking through the door it holds open.

**Fences is the binary somebody else builds.** We compile from the BSD 3-Clause source, publish under our own branding, and **we do not apply the OSMF. Fences binaries are free, for everyone - no revenue threshold, no fee.** If you use Polly and would rather not pay the fee, Fences is a drop-in alternative: change your package references and your namespaces, as described below.

We track the Polly API today and expect to keep doing so, and we may take up changes made upstream. But Fences is under Brighter Command's stewardship now and will evolve to suit its users, so a future major version may diverge from Polly and stop being API-compatible. We will say so plainly when it does; it will not arrive in a patch.

None of this is a criticism of App vNext, who have maintained Polly for years and are entitled to be paid for their work. Publishing our own binaries is exactly what the licence they chose allows. See [ADR 0002](docs/adr/0002-fork-polly-as-fences.md) for the full reasoning.

This README describes the v8 API. The pre-v8 API is still shipped, in the `Paramore.Fences` package; see the [v7 documentation](docs/v7/).

## NuGet packages

All five are published as stable releases from `9.0.0` onwards.

| **Package** | **Replaces** | **About** |
| :---------- | :----------- | :-------- |
| `Paramore.Fences.Core` | `Polly.Core` | The core abstractions and [built-in strategies](docs/strategies/). |
| `Paramore.Fences.Extensions` | `Polly.Extensions` | Dependency injection and [telemetry](docs/advanced/telemetry.md). |
| `Paramore.Fences.RateLimiting` | `Polly.RateLimiting` | Integration with the [`System.Threading.RateLimiting`](https://www.nuget.org/packages/System.Threading.RateLimiting) APIs. |
| `Paramore.Fences.Testing` | `Polly.Testing` | [Testing support](docs/advanced/testing.md). |
| `Paramore.Fences` | `Polly` | The legacy API exposed by versions before version 8. |

The library, the repository and the documentation site are all called **Fences**. Only the package and assembly identifiers carry the `Paramore` prefix, which is the identifier family the rest of the Brighter Command projects use.

## Documentation

This README aims to give a quick overview of some Fences features - including enough to get you started with any resilience strategy. For more detail on any resilience strategy, and many other aspects of Fences, see the [documentation](docs/), which is also published at [brightercommand.github.io/Fences](https://brightercommand.github.io/Fences/).

## Migrating from Polly

For a project on Polly 8.7.0, moving to Fences is a change of package reference and namespace, not a rewrite:

```diff
-<PackageReference Include="Polly.Core" Version="8.7.0" />
+<PackageReference Include="Paramore.Fences.Core" Version="9.0.0" />
```

```diff
-using Polly;
+using Paramore.Fences;
```

Every namespace maps one-for-one, and exactly one public type was renamed. The one thing a find-and-replace over your own source will not fix is the telemetry names, which changed from `Polly` and `resilience.polly.*` to `Paramore.Fences` and `resilience.fences.*`. See [Migrate from Polly](docs/migration-from-polly.md) for the full mapping, and for the one case Fences cannot help with - `Microsoft.Extensions.Http.Resilience` keeps Polly in your dependency graph regardless.

## Quick start

To use Fences, you must provide a callback and execute it using a [**resilience pipeline**](docs/pipelines/). A resilience pipeline combines one or more [**resilience strategies**](docs/strategies/), such as retry, timeout, and rate limiter. Fences uses **builders** to integrate these strategies into a pipeline.

To get started, first add the `Paramore.Fences.Core` package to your project by running the following command:

```sh
dotnet add package Paramore.Fences.Core
```

You can create a `ResiliencePipeline` using the `ResiliencePipelineBuilder` class as shown below:

<!-- snippet: quick-start -->
```cs
// Create an instance of builder that exposes various extensions for adding resilience strategies
ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions()) // Add retry using the default options
    .AddTimeout(TimeSpan.FromSeconds(10)) // Add 10 seconds timeout
    .Build(); // Builds the resilience pipeline

// Execute the pipeline asynchronously
await pipeline.ExecuteAsync(static async token => { /* Your custom logic goes here */ }, cancellationToken);
```
<!-- endSnippet -->

### Dependency injection

If you prefer to define resilience pipelines using [`IServiceCollection`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection), you'll need to install the `Paramore.Fences.Extensions` package:

```sh
dotnet add package Paramore.Fences.Extensions
```

You can then define your resilience pipeline using the `AddResiliencePipeline(...)` extension method as shown:

<!-- snippet: quick-start-di -->
```cs
var services = new ServiceCollection();

// Define a resilience pipeline with the name "my-pipeline"
services.AddResiliencePipeline("my-pipeline", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions())
        .AddTimeout(TimeSpan.FromSeconds(10));
});

// Build the service provider
var serviceProvider = services.BuildServiceProvider();

// Retrieve a ResiliencePipelineProvider that dynamically creates and caches the resilience pipelines
var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();

// Retrieve your resilience pipeline using the name it was registered with
ResiliencePipeline pipeline = pipelineProvider.GetPipeline("my-pipeline");

// Alternatively, you can use keyed services to retrieve the resilience pipeline
pipeline = serviceProvider.GetRequiredKeyedService<ResiliencePipeline>("my-pipeline");

// Execute the pipeline
await pipeline.ExecuteAsync(static async token =>
{
    // Your custom logic goes here
});
```
<!-- endSnippet -->

## Resilience strategies

Fences provides a variety of resilience strategies. Alongside the comprehensive guides for each strategy, the documentation also includes an [overview of the role each strategy plays in resilience engineering](docs/strategies/).

Fences categorizes resilience strategies into two main groups:

### Reactive

These strategies handle specific exceptions thrown or results returned by callbacks executed through the strategy.

| Strategy | Premise | AKA | Mitigation |
| ------------- | ------------- | -------------- | ------------ |
| [**Retry** family](#retry) | Many faults are transient and may self-correct after a short delay. | *Maybe it's just a blip* | Allows configuring automatic retries. |
| [**Circuit-breaker** family](#circuit-breaker) | When a system is seriously struggling, failing fast is better than making users/callers wait. <br/><br/>Protecting a faulting system from overload can help it recover. | *Stop doing it if it hurts* <br/><br/>*Give that system a break* | Breaks the circuit (blocks executions) for a period when faults exceed some pre-configured threshold. |
| [**Fallback**](#fallback) | Things will still fail - plan what you will do when that happens. | *Degrade gracefully* | Defines an alternative value to be returned (or action to be executed) on failure. |
| [**Hedging**](#hedging) | Things can be slow sometimes; plan what you will do when that happens. | *Hedge your bets* | Executes parallel actions when things are slow and waits for the fastest one. |

### Proactive

Unlike reactive strategies, proactive strategies don't focus on handling errors; instead, they focus on what callbacks might throw or return. They can proactively cancel or reject callback execution.

| Strategy | Premise | AKA | Prevention |
| ----------- | ------------- | -------------- | ------------ |
| [**Timeout**](#timeout) | Beyond a certain wait, a success result is unlikely. | *Don't wait forever* | Guarantees the caller won't have to wait beyond the timeout. |
| [**Rate Limiter**](#rate-limiter) | Limiting the rate at which a system handles requests is another way to control load. <br/> <br/> This can apply to the way your system accepts incoming calls, and/or to the way you call downstream services. | *Slow down a bit, will you?* | Constrains executions to not exceed a certain rate. |

Visit [resilience strategies](docs/strategies/) to explore how to configure individual resilience strategies in more detail.

### Retry

<!-- snippet: retry -->
```cs
// Retry using the default options.
// See https://brightercommand.github.io/Fences/strategies/retry#defaults for defaults.
var optionsDefaults = new RetryStrategyOptions();

// For instant retries with no delay
var optionsNoDelay = new RetryStrategyOptions
{
    Delay = TimeSpan.Zero
};

// For advanced control over the retry behavior, including the number of attempts,
// delay between retries, and the types of exceptions to handle.
var optionsComplex = new RetryStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<SomeExceptionType>(),
    BackoffType = DelayBackoffType.Exponential,
    UseJitter = true,  // Adds a random factor to the delay
    MaxRetryAttempts = 4,
    Delay = TimeSpan.FromSeconds(3),
};

// To use a custom function to generate the delay for retries
var optionsDelayGenerator = new RetryStrategyOptions
{
    MaxRetryAttempts = 2,
    DelayGenerator = static args =>
    {
        var delay = args.AttemptNumber switch
        {
            0 => TimeSpan.Zero,
            1 => TimeSpan.FromSeconds(1),
            _ => TimeSpan.FromSeconds(5)
        };

        // This example uses a synchronous delay generator,
        // but the API also supports asynchronous implementations.
        return new ValueTask<TimeSpan?>(delay);
    }
};

// To extract the delay from the result object
var optionsExtractDelay = new RetryStrategyOptions<HttpResponseMessage>
{
    DelayGenerator = static args =>
    {
        if (args.Outcome.Result is HttpResponseMessage responseMessage &&
            TryGetDelay(responseMessage, out TimeSpan delay))
        {
            return new ValueTask<TimeSpan?>(delay);
        }

        // Returning null means the retry strategy will use its internal delay for this attempt.
        return new ValueTask<TimeSpan?>((TimeSpan?)null);
    }
};

// To get notifications when a retry is performed
var optionsOnRetry = new RetryStrategyOptions
{
    MaxRetryAttempts = 2,
    OnRetry = static args =>
    {
        Console.WriteLine("OnRetry, Attempt: {0}", args.AttemptNumber);

        // Event handlers can be asynchronous; here, we return an empty ValueTask.
        return default;
    }
};

// To keep retrying indefinitely or until success use int.MaxValue.
var optionsIndefiniteRetry = new RetryStrategyOptions
{
    MaxRetryAttempts = int.MaxValue,
};

// Add a retry strategy with a RetryStrategyOptions{<TResult>} instance to the pipeline
new ResiliencePipelineBuilder().AddRetry(optionsDefaults);
new ResiliencePipelineBuilder<HttpResponseMessage>().AddRetry(optionsExtractDelay);
```
<!-- endSnippet -->

If all retries fail, a retry strategy rethrows the final exception back to the calling code.

For more details, visit the [retry strategy](docs/strategies/retry.md) documentation.

### Circuit Breaker

<!-- snippet: circuit-breaker -->
```cs
// Circuit breaker with default options.
// See https://brightercommand.github.io/Fences/strategies/circuit-breaker#defaults for defaults.
var optionsDefaults = new CircuitBreakerStrategyOptions();

// Circuit breaker with customized options:
// The circuit will break if more than 50% of actions result in handled exceptions,
// within any 10-second sampling duration, and at least 8 actions are processed.
var optionsComplex = new CircuitBreakerStrategyOptions
{
    FailureRatio = 0.5,
    SamplingDuration = TimeSpan.FromSeconds(10),
    MinimumThroughput = 8,
    BreakDuration = TimeSpan.FromSeconds(30),
    ShouldHandle = new PredicateBuilder().Handle<SomeExceptionType>()
};

// Circuit breaker using BreakDurationGenerator:
// The break duration is dynamically determined based on the properties of BreakDurationGeneratorArguments.
var optionsBreakDurationGenerator = new CircuitBreakerStrategyOptions
{
    FailureRatio = 0.5,
    SamplingDuration = TimeSpan.FromSeconds(10),
    MinimumThroughput = 8,
    BreakDurationGenerator = static args => new ValueTask<TimeSpan>(TimeSpan.FromMinutes(args.FailureCount)),
};

// Handle specific failed results for HttpResponseMessage:
var optionsShouldHandle = new CircuitBreakerStrategyOptions<HttpResponseMessage>
{
    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        .Handle<SomeExceptionType>()
        .HandleResult(response => response.StatusCode == HttpStatusCode.InternalServerError)
};

// Monitor the circuit state, useful for health reporting:
var stateProvider = new CircuitBreakerStateProvider();
var optionsStateProvider = new CircuitBreakerStrategyOptions<HttpResponseMessage>
{
    StateProvider = stateProvider
};

var circuitState = stateProvider.CircuitState;

/*
CircuitState.Closed - Normal operation; actions are executed.
CircuitState.Open - Circuit is open; actions are blocked.
CircuitState.HalfOpen - Recovery state after break duration expires; actions are permitted.
CircuitState.Isolated - Circuit is manually held open; actions are blocked.
*/

// Manually control the Circuit Breaker state:
var manualControl = new CircuitBreakerManualControl();
var optionsManualControl = new CircuitBreakerStrategyOptions
{
    ManualControl = manualControl
};

// Manually isolate a circuit, e.g., to isolate a downstream service.
await manualControl.IsolateAsync();

// Manually close the circuit to allow actions to be executed again.
await manualControl.CloseAsync();

// Add a circuit breaker strategy with a CircuitBreakerStrategyOptions{<TResult>} instance to the pipeline
new ResiliencePipelineBuilder().AddCircuitBreaker(optionsDefaults);
new ResiliencePipelineBuilder<HttpResponseMessage>().AddCircuitBreaker(optionsStateProvider);
```
<!-- endSnippet -->

For more details, visit the [circuit breaker strategy](docs/strategies/circuit-breaker.md) documentation.

### Fallback

<!-- snippet: fallback -->
```cs
// A fallback/substitute value if an operation fails.
var optionsSubstitute = new FallbackStrategyOptions<UserAvatar>
{
    ShouldHandle = new PredicateBuilder<UserAvatar>()
        .Handle<SomeExceptionType>()
        .HandleResult(r => r is null),
    FallbackAction = static args => Outcome.FromResultAsValueTask(UserAvatar.Blank)
};

// Use a dynamically generated value if an operation fails.
var optionsFallbackAction = new FallbackStrategyOptions<UserAvatar>
{
    ShouldHandle = new PredicateBuilder<UserAvatar>()
        .Handle<SomeExceptionType>()
        .HandleResult(r => r is null),
    FallbackAction = static args =>
    {
        var avatar = UserAvatar.GetRandomAvatar();
        return Outcome.FromResultAsValueTask(avatar);
    }
};

// Use a default or dynamically generated value, and execute an additional action if the fallback is triggered.
var optionsOnFallback = new FallbackStrategyOptions<UserAvatar>
{
    ShouldHandle = new PredicateBuilder<UserAvatar>()
        .Handle<SomeExceptionType>()
        .HandleResult(r => r is null),
    FallbackAction = static args =>
    {
        var avatar = UserAvatar.GetRandomAvatar();
        return Outcome.FromResultAsValueTask(UserAvatar.Blank);
    },
    OnFallback = static args =>
    {
        // Add extra logic to be executed when the fallback is triggered, such as logging.
        return default; // Returns an empty ValueTask
    }
};

// Add a fallback strategy with a FallbackStrategyOptions<TResult> instance to the pipeline
new ResiliencePipelineBuilder<UserAvatar>().AddFallback(optionsOnFallback);
```
<!-- endSnippet -->

For more details, visit the [fallback strategy](docs/strategies/fallback.md) documentation.

### Hedging

<!-- snippet: hedging -->
```cs
// Hedging with default options.
// See https://brightercommand.github.io/Fences/strategies/hedging#defaults for defaults.
var optionsDefaults = new HedgingStrategyOptions<HttpResponseMessage>();

// A customized hedging strategy that retries up to 3 times if the execution
// takes longer than 1 second or if it fails due to an exception or returns an HTTP 500 Internal Server Error.
var optionsComplex = new HedgingStrategyOptions<HttpResponseMessage>
{
    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        .Handle<SomeExceptionType>()
        .HandleResult(response => response.StatusCode == HttpStatusCode.InternalServerError),
    MaxHedgedAttempts = 3,
    Delay = TimeSpan.FromSeconds(1),
    ActionGenerator = static args =>
    {
        Console.WriteLine("Preparing to execute hedged action.");

        // Return a delegate function to invoke the original action with the action context.
        // Optionally, you can also create a completely new action to be executed.
        return () => args.Callback(args.ActionContext);
    }
};

// Subscribe to hedging events.
var optionsOnHedging = new HedgingStrategyOptions<HttpResponseMessage>
{
    OnHedging = static args =>
    {
        Console.WriteLine($"OnHedging: Attempt number {args.AttemptNumber}");
        return default;
    }
};

// Add a hedging strategy with a HedgingStrategyOptions<TResult> instance to the pipeline
new ResiliencePipelineBuilder<HttpResponseMessage>().AddHedging(optionsDefaults);
```
<!-- endSnippet -->

If all hedged attempts fail, the hedging strategy will either re-throw the original exception or return the original failed result to the caller.

For more details, visit the [hedging strategy](docs/strategies/hedging.md) documentation.

### Timeout

The timeout resilience strategy assumes delegates you execute support [co-operative cancellation](https://learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads). You must use `Execute/Async(...)` overloads taking a `CancellationToken`, and the executed delegate must honor that `CancellationToken`.

<!-- snippet: timeout -->
```cs
// To add a timeout with a custom TimeSpan duration
new ResiliencePipelineBuilder().AddTimeout(TimeSpan.FromSeconds(3));

// Timeout using the default options.
// See https://brightercommand.github.io/Fences/strategies/timeout#defaults for defaults.
var optionsDefaults = new TimeoutStrategyOptions();

// To add a timeout using a custom timeout generator function
var optionsTimeoutGenerator = new TimeoutStrategyOptions
{
    TimeoutGenerator = static args =>
    {
        // Note: the timeout generator supports asynchronous operations
        return new ValueTask<TimeSpan>(TimeSpan.FromSeconds(123));
    }
};

// To add a timeout and listen for timeout events
var optionsOnTimeout = new TimeoutStrategyOptions
{
    TimeoutGenerator = static args =>
    {
        // Note: the timeout generator supports asynchronous operations
        return new ValueTask<TimeSpan>(TimeSpan.FromSeconds(123));
    },
    OnTimeout = static args =>
    {
        Console.WriteLine($"{args.Context.OperationKey}: Execution timed out after {args.Timeout.TotalSeconds} seconds.");
        return default;
    }
};

// Add a timeout strategy with a TimeoutStrategyOptions instance to the pipeline
new ResiliencePipelineBuilder().AddTimeout(optionsDefaults);
```
<!-- endSnippet -->

Timeout strategies throw `TimeoutRejectedException` when a timeout occurs.

For more details, visit the [timeout strategy](docs/strategies/timeout.md) documentation.

### Rate Limiter

<!-- snippet: rate-limiter -->
```cs
// Add rate limiter with default options.
// See https://brightercommand.github.io/Fences/strategies/rate-limiter#defaults for defaults.
new ResiliencePipelineBuilder()
    .AddRateLimiter(new RateLimiterStrategyOptions());

// Create a rate limiter to allow a maximum of 100 concurrent executions and a queue of 50.
new ResiliencePipelineBuilder()
    .AddConcurrencyLimiter(100, 50);

// Create a rate limiter that allows 100 executions per minute.
new ResiliencePipelineBuilder()
    .AddRateLimiter(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 100,
            SegmentsPerWindow = 4,
            Window = TimeSpan.FromMinutes(1)
        }));
```
<!-- endSnippet -->

Rate limiter strategy throws `RateLimiterRejectedException` if execution is rejected.

For more details, visit the [rate limiter strategy](docs/strategies/rate-limiter.md) documentation.

## Chaos engineering

[Simmy](https://github.com/Polly-Contrib/Simmy), a chaos engineering library, was integrated directly into the core in Polly 8.3.0, and Fences inherits it. For more information, please refer to the dedicated [chaos engineering documentation](docs/chaos/).

## Next steps

To learn more about Fences, visit the [documentation](docs/).

## Samples

- [Samples](samples/README.md): Samples in this repository that serve as an introduction to Fences.
- [Polly-Samples](https://github.com/App-vNext/Polly-Samples): Practical examples written for Polly. The v8 API is unchanged in Fences, so they apply once the package references and namespaces are swapped.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md). Fences is maintained by [Brighter Command](https://github.com/BrighterCommand), alongside [Brighter](https://github.com/BrighterCommand/Brighter) and [Darker](https://github.com/BrighterCommand/Darker).

## License

Licensed under the terms of the [New BSD License](https://opensource.org/license/bsd-3-clause/), the same licence as Polly. App vNext's copyright notice is retained in [`LICENSE`](LICENSE) as that licence requires; see [`ACKNOWLEDGEMENTS.md`](ACKNOWLEDGEMENTS.md) for credit to Polly's authors and contributors.
