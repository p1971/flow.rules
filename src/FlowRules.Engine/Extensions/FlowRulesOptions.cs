using System;

namespace FlowRules.Engine.Extensions;

/// <summary>
/// Options class used to initialise the FlowRules.Engine.
/// </summary>
public class FlowRulesOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether telemetry should be emitted for this policy.
    /// Other policies are unaffected.
    /// </summary>
    public bool ExportTelemetry { get; set; } = true;

    /// <summary>
    /// Gets or sets the results repository to use. The repository is registered with a
    /// scoped lifetime and shared by all policies for the same request type. It replaces
    /// the no-op default; conflicting custom repository choices are rejected.
    /// To use a different lifetime or a factory registration, register
    /// <c>IPolicyResultsRepository&lt;T&gt;</c> before calling <c>AddFlowRules</c>.
    /// </summary>
    public Type? ResultsRepository { get; set; }
}
