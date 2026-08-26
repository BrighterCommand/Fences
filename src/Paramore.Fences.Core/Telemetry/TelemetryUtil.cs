namespace Paramore.Fences.Telemetry;

internal static class TelemetryUtil
{
    internal const string FencesDiagnosticSource = "Paramore.Fences";

    internal const string ExecutionAttempt = "ExecutionAttempt";

    internal const string PipelineExecuting = "PipelineExecuting";

    internal const string PipelineExecuted = "PipelineExecuted";

    public static void ReportExecutionAttempt<TResult>(
        ResilienceStrategyTelemetry telemetry,
        ResilienceContext context,
        Outcome<TResult> outcome,
        int attempt,
        TimeSpan executionTime,
        bool handled)
    {
        ReportAttempt(
            telemetry,
            new(handled ? ResilienceEventSeverity.Warning : ResilienceEventSeverity.Information, ExecutionAttempt),
            context,
            outcome,
            new ExecutionAttemptArguments(attempt, executionTime, handled));
    }

    public static void ReportFinalExecutionAttempt<TResult>(
        ResilienceStrategyTelemetry telemetry,
        ResilienceContext context,
        Outcome<TResult> outcome,
        int attempt,
        TimeSpan executionTime,
        bool handled)
    {
        ReportAttempt(
            telemetry,
            new(handled ? ResilienceEventSeverity.Error : ResilienceEventSeverity.Information, ExecutionAttempt),
            context,
            outcome,
            new ExecutionAttemptArguments(attempt, executionTime, handled));
    }

    private static void ReportAttempt<TResult>(
        ResilienceStrategyTelemetry telemetry,
        ResilienceEvent resilienceEvent,
        ResilienceContext context,
        Outcome<TResult> outcome,
        ExecutionAttemptArguments args)
    {
        if (telemetry.Enabled)
        {
            telemetry.Report(resilienceEvent, context, outcome, args);
        }
    }
}
