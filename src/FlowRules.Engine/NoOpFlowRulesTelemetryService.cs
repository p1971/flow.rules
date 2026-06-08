using System;
using System.Diagnostics;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine;

/// <inheritdoc/>
internal sealed class NoOpFlowRulesTelemetryService : IFlowRulesTelemetryService
{
    /// <inheritdoc/>
    public void PolicyExecution<T>(Policy<T> policy, Guid contextId, string correlationId, TimeSpan elapsedTime)
        where T : class
    {
    }

    /// <inheritdoc/>
    public void RuleExecution<T>(Policy<T> policy, Rule<T> rule, Guid contextId, string correlationId, TimeSpan elapsedTime)
        where T : class
    {
    }

    /// <inheritdoc/>
    public Activity? StartActivity<T>(Policy<T> policy, Guid contextId, string correlationId)
        where T : class => null;

    /// <inheritdoc/>
    public Activity? StartActivity<T>(Rule<T> rule, Guid contextId, string correlationId)
        where T : class => null;

    /// <inheritdoc/>
    public void SetSuccess(Activity? activity)
    {
    }

    /// <inheritdoc/>
    public void SetFailure(Activity? activity, string errorMessage)
    {
    }
}
