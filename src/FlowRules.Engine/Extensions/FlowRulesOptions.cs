using System;

namespace FlowRules.Engine.Extensions;

/// <summary>
/// Options class used to initialise the FlowRules.Engine.
/// </summary>
public class FlowRulesOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether FlowRules telemetry should be exported.
    /// </summary>
    public bool ExportTelemetry { get; set; } = true;

    /// <summary>
    /// Gets or sets the results repository to use. The repository is registered with a
    /// scoped lifetime. To use a different lifetime, register
    /// <c>IPolicyResultsRepository&lt;T&gt;</c> before calling <c>AddFlowRules</c>.
    /// </summary>
    public Type? ResultsRepository { get; set; }
}
