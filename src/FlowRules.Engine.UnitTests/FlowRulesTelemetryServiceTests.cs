using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

using FlowRules.Engine.Models;

using Xunit;

namespace FlowRules.Engine.UnitTests;

public class FlowRulesTelemetryServiceTests
{
    [Fact]
    public void Policy_Activity_Should_Have_Descriptive_Name_And_Execution_Details()
    {
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == FlowRulesTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Create()
            .WithId("P001")
            .WithName("Person policy")
            .WithRule("R001", "Person rule", (_, _) => ValueTask.FromResult(true))
            .Build();
        Guid executionContextId = Guid.NewGuid();
        FlowRulesTelemetryService telemetry = new();

        using Activity? activity = telemetry.StartActivity(policy, executionContextId, "correlation-1");

        Assert.NotNull(activity);
        Assert.Equal("flowrules.policy.execute", activity.OperationName);
        Assert.Equal("Policy P001: Person policy", activity.DisplayName);
        Assert.Equal("P001", activity.GetTagItem("policy.id"));
        Assert.Equal("Person policy", activity.GetTagItem("policy.name"));
        Assert.Equal(executionContextId, activity.GetTagItem("execution.context.id"));
        Assert.Equal("correlation-1", activity.GetTagItem("correlation.id"));
    }

    [Fact]
    public void Rule_Activity_Should_Include_Policy_Rule_And_Failure_Details()
    {
        using ActivityListener listener = CreateActivityListener();
        ActivitySource.AddActivityListener(listener);

        Policy<PersonDataModel> policy = CreatePolicy();
        Rule<PersonDataModel> rule = Assert.Single(policy.Rules);
        FlowRulesTelemetryService telemetry = new();
        InvalidOperationException exception = new("Rule exploded");

        using Activity? activity = telemetry.StartActivity(
            policy,
            rule,
            Guid.NewGuid(),
            "correlation-1");
        telemetry.SetFailure(activity, exception.Message, exception);

        Assert.NotNull(activity);
        Assert.Equal("flowrules.rule.execute", activity.OperationName);
        Assert.Equal("Rule R001: Person rule", activity.DisplayName);
        Assert.Equal("P001", activity.GetTagItem("policy.id"));
        Assert.Equal("Person policy", activity.GetTagItem("policy.name"));
        Assert.Equal("R001", activity.GetTagItem("rule.id"));
        Assert.Equal("Person rule", activity.GetTagItem("rule.name"));
        Assert.Equal(false, activity.GetTagItem("rule.passed"));
        Assert.Equal("Rule exploded", activity.GetTagItem("rule.fail"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);

        ActivityEvent exceptionEvent = Assert.Single(activity.Events);
        Assert.Equal("exception", exceptionEvent.Name);
        Assert.Contains(exceptionEvent.Tags, tag =>
            tag.Key == "exception.type" && tag.Value?.ToString() == typeof(InvalidOperationException).FullName);
        Assert.Contains(exceptionEvent.Tags, tag =>
            tag.Key == "exception.message" && tag.Value?.ToString() == exception.Message);
    }

    [Theory]
    [InlineData(true, ActivityStatusCode.Ok)]
    [InlineData(false, ActivityStatusCode.Error)]
    public void Policy_Result_Should_Be_Added_To_Activity(bool passed, ActivityStatusCode expectedStatus)
    {
        using ActivityListener listener = CreateActivityListener();
        ActivitySource.AddActivityListener(listener);

        Policy<PersonDataModel> policy = CreatePolicy();
        FlowRulesTelemetryService telemetry = new();

        using Activity? activity = telemetry.StartActivity(policy, Guid.NewGuid(), "correlation-1");
        telemetry.SetPolicyResult(activity, passed);

        Assert.NotNull(activity);
        Assert.Equal(passed, activity.GetTagItem("policy.passed"));
        Assert.Equal(expectedStatus, activity.Status);
    }

    [Fact]
    public void Metrics_Should_Only_Use_Stable_Policy_And_Rule_Dimensions()
    {
        string policyId = Guid.NewGuid().ToString();
        List<(string InstrumentName, Dictionary<string, object?> Tags)> measurements = [];

        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == FlowRulesTelemetry.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            Dictionary<string, object?> copiedTags = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                copiedTags.Add(tag.Key, tag.Value);
            }

            if (copiedTags.TryGetValue("policy.id", out object? measuredPolicyId)
                && measuredPolicyId?.ToString() == policyId)
            {
                measurements.Add((instrument.Name, copiedTags));
            }
        });
        listener.Start();

        Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Create()
            .WithId(policyId)
            .WithName("Person policy")
            .WithRule("R001", "Person rule", (_, _) => ValueTask.FromResult(true))
            .Build();
        Rule<PersonDataModel> rule = Assert.Single(policy.Rules);
        FlowRulesTelemetryService telemetry = new();

        telemetry.PolicyExecution(policy, TimeSpan.FromMilliseconds(10));
        telemetry.RuleExecution(policy, rule, TimeSpan.FromMilliseconds(5));

        Assert.Collection(
            measurements,
            policyMeasurement =>
            {
                Assert.Equal("flowrules.policy.duration", policyMeasurement.InstrumentName);
                Assert.Equal(["policy.id", "policy.name"], policyMeasurement.Tags.Keys);
            },
            ruleMeasurement =>
            {
                Assert.Equal("flowrules.rule.duration", ruleMeasurement.InstrumentName);
                Assert.Equal(
                    ["policy.id", "policy.name", "rule.id", "rule.name"],
                    ruleMeasurement.Tags.Keys);
            });
    }


    private static ActivityListener CreateActivityListener() =>
        new()
        {
            ShouldListenTo = source => source.Name == FlowRulesTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };

    private static Policy<PersonDataModel> CreatePolicy() =>
        PolicyBuilder<PersonDataModel>.Create()
            .WithId("P001")
            .WithName("Person policy")
            .WithRule("R001", "Person rule", (_, _) => ValueTask.FromResult(true))
            .Build();
}
