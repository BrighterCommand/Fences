# Chaos Example

This example demonstrates how to use [chaos engineering](https://www.pollydocs.org/chaos) tools to inject chaos into HTTP client communication.
The HTTP client communicates with the `https://jsonplaceholder.typicode.com/todos` endpoint.

> [!IMPORTANT]
> **This sample uses Polly's API, not Fences', and that is deliberate.** It configures chaos through
> `Microsoft.Extensions.Http.Resilience`, whose `AddResilienceHandler` hands its callback a Polly
> `ResiliencePipelineBuilder<HttpResponseMessage>`. Fences cannot substitute for Polly there, so the
> project takes an explicit `Polly.Core` reference and the links above go to Polly's documentation.
> See [Migrate from Polly](../../docs/migration-from-polly.md) for the bounded form of that claim.
> Every other sample in this directory uses Fences.

To test the application:

- Run the app using the `dotnet run` command.
- Access the root endpoint `https://localhost:62683` and refresh it multiple times.
- Observe the logs in out console window. You should see chaos injection and also mitigation of chaos by resilience strategies.
