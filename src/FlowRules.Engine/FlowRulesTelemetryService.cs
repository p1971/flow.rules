using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine;

/// <inheritdoc/>
internal sealed class FlowRulesTelemetryService : IFlowRulesTelemetryService
{
    private const string PolicyIdKey = "policy.id";
    private const string PolicyNameKey = "policy.name";
    private const string RuleIdKey = "rule.id";
    private const string RuleNameKey = "rule.name";
    private const string RuleFailKey = "rule.fail";
    private const string CorrelationIdKey = "correlationId";
    private const string ExecutionContextIdKey = "executionContextId";

    private static readonly ActivitySource PolicyActivitySource = new("FlowRules.Policy");

    private static readonly Meter FlowMeter = new("FlowRules");

    private static readonly Histogram<double> PolicyHistogram;
    private static readonly Histogram<double> RuleHistogram;

    static FlowRulesTelemetryService()
    {
        PolicyHistogram = FlowMeter.CreateHistogram<double>(
            name: "policy.duration",
            unit: "ms",
            description: "Policy execution elapsed time in milliseconds");

        RuleHistogram = FlowMeter.CreateHistogram<double>(
            name: "policy.rule.duration",
            unit: "ms",
            description: "Rule execution elapsed time in milliseconds");
    }

    /// <inheritdoc/>
    public void PolicyExecution<T>(Policy<T> policy, Guid contextId, string correlationId, TimeSpan elapsedTime)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(policy);

        PolicyHistogram.Record(elapsedTime.TotalMilliseconds, new TagList
        {
            { PolicyIdKey, policy.Id },
            { PolicyNameKey, policy.Name },
            { ExecutionContextIdKey, contextId },
            { CorrelationIdKey, correlationId },
        });
    }

    /// <inheritdoc/>
    public void RuleExecution<T>(Policy<T> policy, Rule<T> rule, Guid contextId, string correlationId, TimeSpan elapsedTime)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(rule);

        RuleHistogram.Record(elapsedTime.TotalMilliseconds, new TagList
        {
            { PolicyIdKey, policy.Id },
            { PolicyNameKey, policy.Name },
            { RuleIdKey, rule.Id },
            { RuleNameKey, rule.Name },
            { ExecutionContextIdKey, contextId },
            { CorrelationIdKey, correlationId },
        });
    }

    /// <inheritdoc/>
    public Activity? StartActivity<T>(Policy<T> policy, Guid contextId, string correlationId)
        where T : class
    {
        Activity? activity = PolicyActivitySource.StartActivity(policy.Name);

        activity?.SetTag(PolicyIdKey, policy.Id);
        activity?.SetTag(PolicyNameKey, policy.Name);
        activity?.SetTag(ExecutionContextIdKey, contextId);
        activity?.SetTag(CorrelationIdKey, correlationId);

        return activity;
    }

    /// <inheritdoc/>
    public Activity? StartActivity<T>(Rule<T> rule, Guid contextId, string correlationId)
        where T : class
    {
        Activity? activity = PolicyActivitySource.StartActivity(rule.Name);

        activity?.SetTag(RuleIdKey, rule.Id);
        activity?.SetTag(RuleNameKey, rule.Name);
        activity?.SetTag(ExecutionContextIdKey, contextId);
        activity?.SetTag(CorrelationIdKey, correlationId);

        return activity;
    }

    public void SetSuccess(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    public void SetFailure(Activity? activity, string errorMessage)
    {
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag(RuleFailKey, errorMessage);
    }
}
