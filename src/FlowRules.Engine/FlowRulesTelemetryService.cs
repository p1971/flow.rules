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
    private const string PolicyPassedKey = "policy.passed";
    private const string RuleIdKey = "rule.id";
    private const string RuleNameKey = "rule.name";
    private const string RulePassedKey = "rule.passed";
    private const string RuleFailKey           = "rule.fail";
    private const string CorrelationIdKey      = "correlation.id";
    private const string ExecutionContextIdKey = "execution.context.id";

    // Stable Activity operation names; display names add policy and rule context in trace views.
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
    public void PolicyExecution<T>(Policy<T> policy, TimeSpan elapsedTime)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(policy);

        PolicyHistogram.Record(elapsedTime.TotalMilliseconds, new TagList
        {
            { PolicyIdKey, policy.Id },
            { PolicyNameKey, policy.Name },
        });
    }

    /// <inheritdoc/>
    public void RuleExecution<T>(Policy<T> policy, Rule<T> rule, TimeSpan elapsedTime)
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
        });
    }

    /// <inheritdoc/>
    public Activity? StartActivity<T>(Policy<T> policy, Guid contextId, string correlationId)
        where T : class
    {
        Activity? activity = PolicyActivitySource.StartActivity(name: PolicyExecuteOperation);

        if (activity is not null)
        {
            activity.DisplayName = $"Policy {policy.Id}: {policy.Name}";
        }

        activity?.SetTag(PolicyIdKey, policy.Id);
        activity?.SetTag(PolicyNameKey, policy.Name);
        activity?.SetTag(ExecutionContextIdKey, contextId);
        activity?.SetTag(CorrelationIdKey, correlationId);

        return activity;
    }

    /// <inheritdoc/>
    public Activity? StartActivity<T>(Policy<T> policy, Rule<T> rule, Guid contextId, string correlationId)
        where T : class
    {
        Activity? activity = PolicyActivitySource.StartActivity(name: RuleExecuteOperation);

        if (activity is not null)
        {
            activity.DisplayName = $"Rule {rule.Id}: {rule.Name}";
        }

        activity?.SetTag(PolicyIdKey, policy.Id);
        activity?.SetTag(PolicyNameKey, policy.Name);
        activity?.SetTag(RuleIdKey, rule.Id);
        activity?.SetTag(RuleNameKey, rule.Name);
        activity?.SetTag(ExecutionContextIdKey, contextId);
        activity?.SetTag(CorrelationIdKey, correlationId);

        return activity;
    }

    /// <inheritdoc/>
    public void SetPolicyResult(Activity? activity, bool passed)
    {
        activity?.SetTag(PolicyPassedKey, passed);
        activity?.SetStatus(passed ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
    }

    /// <inheritdoc/>
    public void SetSuccess(Activity? activity)
    {
        activity?.SetTag(RulePassedKey, true);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <inheritdoc/>
    public void SetFailure(Activity? activity, string errorMessage, Exception? exception = null)
    {
        activity?.SetTag(RulePassedKey, false);
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag(RuleFailKey, errorMessage);

        if (activity is not null && exception is not null)
        {
            activity.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    { "exception.type", exception.GetType().FullName },
                    { "exception.message", exception.Message },
                    { "exception.stacktrace", exception.ToString() },
                }));
        }
    }
}
