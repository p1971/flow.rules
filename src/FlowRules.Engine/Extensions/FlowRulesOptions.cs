using System;

namespace FlowRules.Engine.Extensions;

/// <summary>
/// Options class used to initialise the FlowRules.Engine.
/// </summary>
public class FlowRulesOptions
{
    /// <summary>
    /// Gets or sets the results repository to use.
    /// </summary>
    public Type ResultsRepository { get; set; }

    /// <summary>
    /// Gets or sets the repository to use for storing the policy audit data.
    /// </summary>
    public Type PolicyAuditRepository { get; set; }
}
