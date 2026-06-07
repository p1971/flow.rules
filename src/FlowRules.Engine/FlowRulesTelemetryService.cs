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
    private const string RuleFailKey           = "rule.fail";
    private const string CorrelationIdKey      = "correlation.id";
    private const string ExecutionContextIdKey = "execution.context.id";

    // Stable operation names for span grouping in tracing backends (Jaeger, Grafana Tempo, etc.).
    private const string PolicyExecuteOperation = "flowrules.policy.execute";
    private const string RuleExecuteOperation   = "flowrules.rule.execute";

    private static readonly ActivitySource PolicyActivitySource = new(FlowRulesTelemetry.ActivitySourceName);

    private static readonly Meter FlowMeter = new(FlowRulesTelemetry.MeterName);

    private static readonly Histogram<double> PolicyHistogram;
    private static readonly Histogram<double> RuleHistogram;

    static FlowRulesTelemetryService()
    {
        PolicyHistogram = FlowMeter.CreateHistogram<double>(
            name: "flowrules.policy.duration",
            unit: "ms",
            description: "Policy execution elapsed time in milliseconds");

        RuleHistogram = FlowMeter.CreateHistogram<double>(
            name: "flowrules.rule.duration",
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
        Activity? activity = PolicyActivitySource.StartActivity(name: PolicyExecuteOperation);

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
        Activity? activity = PolicyActivitySource.StartActivity(name: RuleExecuteOperation);

        activity?.SetTag(RuleIdKey, rule.Id);
        activity?.SetTag(RuleNameKey, rule.Name);
        activity?.SetTag(ExecutionContextIdKey, contextId);
        activity?.SetTag(CorrelationIdKey, correlationId);

        return activity;
    }

    /// <inheritdoc/>
    public void SetSuccess(Activity? activity) =>
        activity?.SetStatus(ActivityStatusCode.Ok);

    /// <inheritdoc/>
    public void SetFailure(Activity? activity, string errorMessage)
    {
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag(RuleFailKey, errorMessage);
    }
}
