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
    /// Gets or sets the results repository to use.
    /// </summary>
    public Type? ResultsRepository { get; set; }
}
