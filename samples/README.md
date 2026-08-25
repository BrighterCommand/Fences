# Fences Samples

This directory contains a solution with basic examples demonstrating the creation and utilization of Fences strategies.

- [`Intro`](./Intro) - This section serves as an introduction to Fences. It demonstrates how to use `ResiliencePipelineBuilder` to create a `ResiliencePipeline`, which can be used to execute various user-provided callbacks.
- [`GenericPipelines`](./GenericPipelines) - This example showcases how to use `ResiliencePipelineBuilder<T>` to create a generic `ResiliencePipeline<T>`.
- [`Retries`](./Retries) - This part explains how to configure a retry resilience strategy.
- [`Extensibility`](./Extensibility) - In this part, you can learn how Fences can be extended with custom resilience strategies.
- [`DependencyInjection`](./DependencyInjection) - This section demonstrates the integration of Fences with `IServiceCollection`.
- [`Chaos`](./Chaos) - Simple web application that communicates with an external service using HTTP client. It uses chaos strategies to inject chaos into HTTP client calls. **This one deliberately uses Polly rather than Fences** - see its README for why.

These examples are designed as a quick-start guide to Fences. For more advanced scenarios, the [Polly-Samples](https://github.com/App-vNext/Polly-Samples) repository is written for Polly, but the v8 API is unchanged in Fences, so those samples apply once the package references and namespaces are swapped.
