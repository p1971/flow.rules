using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Extensions.Logging;

namespace FlowRules.Engine;

/// <inheritdoc />
internal class PolicyManager<T>(
    Policy<T> policy,
    IPolicyResultsRepository<T> resultsRepository,
    IFlowRulesTelemetryService flowRulesEventCounterSource,
    ILogger<PolicyManager<T>> logger) : IPolicyManager<T>
        where T : class
{
    private readonly Lazy<Dictionary<string, Rule<T>>> _rulesByIdCache =
        new(() => policy.Rules.ToDictionary(r => r.Id));

    private readonly bool _persistResults = resultsRepository is not DefaultPolicyResultsRepository<T>;

    /// <inheritdoc />
    public async ValueTask<PolicyExecutionResult> Execute(
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
    {
        logger.LogPolicyStartMessage(policy.Id, policy.Name, executionContextId);

        long startTime = TimeProvider.System.GetTimestamp();

        RuleExecutionResult[] response = await Execute(executionContextId, correlationId, request, cancellationToken);

        bool passed = true;

        foreach (RuleExecutionResult ruleExecutionResult in response)
        {
            if (!ruleExecutionResult.Passed)
            {
                passed = false;
                break;
            }
        }

        PolicyExecutionResult policyExecutionResult =
            new()
            {
                RuleContextId = executionContextId,
                CorrelationId = correlationId,
                PolicyId = policy.Id,
                PolicyName = policy.Name,
                Version = policy.Version,
                RuleExecutionResults = response,
                Passed = passed
            };

        if (_persistResults)
        {
            await TryPersistResults(request, policyExecutionResult);
        }

        flowRulesEventCounterSource.PolicyExecution<T>(
            policy,
            executionContextId,
            correlationId,
            TimeProvider.System.GetElapsedTime(startTime));

        return policyExecutionResult;
    }

    /// <inheritdoc />
    public async ValueTask<RuleExecutionResult> Execute(
        string ruleId,
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
    {
        logger.LogRuleStartMessage(ruleId, executionContextId);

        Rule<T> rule = _rulesByIdCache.Value.TryGetValue(ruleId, out Rule<T>? r)
               ? r
               : throw new InvalidOperationException($"No rule with id [{ruleId}] was found.");

        return await ExecuteRule(rule, executionContextId, correlationId, request, cancellationToken);
    }

    private async ValueTask TryPersistResults(T request, PolicyExecutionResult policyExecutionResult)
    {
        try
        {
            await resultsRepository.PersistResults(request, policyExecutionResult);
        }
        catch (Exception ex)
        {
            logger.LogExceptionWritingToRepository(ex, resultsRepository.GetType().Name, policyExecutionResult.RuleContextId);
        }
    }

    private async ValueTask<RuleExecutionResult[]> Execute(
        Guid executionContextId,
        string correlationId,
        T request,
        CancellationToken cancellationToken)
    {
        RuleExecutionResult[] ruleExecutionResults = new RuleExecutionResult[policy.Rules.Count];

        using Activity? activity = flowRulesEventCounterSource.StartActivity(policy, executionContextId, correlationId);

        for (int index = 0; index < policy.Rules.Count; index++)
        {
            ruleExecutionResults[index] = await ExecuteRule(policy.Rules[index], executionContextId, correlationId, request, cancellationToken);
        }

        return ruleExecutionResults;
    }

    private async ValueTask<RuleExecutionResult> ExecuteRule(
        Rule<T> rule,
        Guid executionContextId,
        string correlationId,
        T request,
        CancellationToken cancellationToken)
    {
        logger.LogExecutionPolicy(rule.Id, rule.Name, executionContextId);

        long stampStart = TimeProvider.System.GetTimestamp();
        bool passed = false;
        string? message = null;
        Exception? exception = null;
        TimeSpan elapsed;

        using Activity? activity = flowRulesEventCounterSource.StartActivity(rule, executionContextId, correlationId);

        try
        {
            ValueTask<bool> ruleExecution = rule.Source.Invoke(request, cancellationToken);
            passed = ruleExecution.IsCompletedSuccessfully
                ? ruleExecution.Result
                : await ruleExecution;

            if (passed)
            {
                flowRulesEventCounterSource.SetSuccess(activity);
            }
            else
            {
                message = rule.FailureMessage?.Invoke(request) ?? string.Empty;
                flowRulesEventCounterSource.SetFailure(activity, message);
            }
        }
        catch (OperationCanceledException)
        {
            message = "Rule execution was cancelled.";
            flowRulesEventCounterSource.SetFailure(activity, "cancelled");
            logger.LogRuleCancelled(rule.Id, rule.Name);
        }
        catch (Exception ex)
        {
            exception = ex;
            message = ex.Message;
            flowRulesEventCounterSource.SetFailure(activity, message);
            logger.LogExceptionForRule(ex, rule.Id, rule.Name);
        }
        finally
        {
            elapsed = TimeProvider.System.GetElapsedTime(stampStart);
            flowRulesEventCounterSource.RuleExecution<T>(policy, rule, executionContextId, correlationId, elapsed);
        }

        return new RuleExecutionResult(
            rule.Id,
            rule.Name,
            passed,
            elapsed,
            rule.Description,
            message,
            exception);
    }
}
