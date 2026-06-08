namespace FlowRules.Engine;

/// <summary>
/// Well-known names for FlowRules OpenTelemetry instrumentation.
/// Use these when registering meters and activity sources with the OTEL SDK
/// to ensure the registration matches what the engine emits.
/// </summary>
public static class FlowRulesTelemetry
{
    /// <summary>
    /// The name of the <see cref="System.Diagnostics.Metrics.Meter"/> used by the engine.
    /// Register with <c>.AddMeter(FlowRulesTelemetry.MeterName)</c>.
    /// </summary>
    public const string MeterName = "FlowRules.Engine";

    /// <summary>
    /// The name of the <see cref="System.Diagnostics.ActivitySource"/> used by the engine.
    /// Register with <c>.AddSource(FlowRulesTelemetry.ActivitySourceName)</c>.
    /// </summary>
    public const string ActivitySourceName = "FlowRules.Engine";
}
