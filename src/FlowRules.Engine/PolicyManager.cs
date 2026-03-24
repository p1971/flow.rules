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
public class PolicyManager<T>(Policy<T> policy, IPolicyResultsRepository<T> resultsRepository, IFlowRulesTelemetryService flowRulesEventCounterSource, ILogger<PolicyManager<T>> logger) : IPolicyManager<T>
    where T : class
{
    private readonly Lazy<Dictionary<string, Rule<T>>> _rulesByIdCache =
        new(() => policy.Rules.ToDictionary(r => r.Id));

    /// <inheritdoc />
    public async Task<PolicyExecutionResult> Execute(
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
    {
        logger.LogPolicyStartMessage(policy.Id, policy.Name, executionContextId);

        long startTime = TimeProvider.System.GetTimestamp();

        IList<RuleExecutionResult> response = await Execute(executionContextId, correlationId, request, cancellationToken);

        PolicyExecutionResult policyExecutionResult =
            new()
            {
                RuleContextId = executionContextId,
                CorrelationId = correlationId,
                PolicyId = policy.Id,
                PolicyName = policy.Name,
                Version = policy.GetType().Assembly.GetName().Version?.ToString(4),
                RuleExecutionResults = [.. response],
                Passed = response.All(r => r.Passed)
            };

        await TryPersistResults(request, policyExecutionResult);

        flowRulesEventCounterSource.PolicyExecution<T>(
            policy,
            executionContextId,
            correlationId,
            TimeProvider.System.GetElapsedTime(startTime));

        return policyExecutionResult;
    }

    /// <inheritdoc />
    public async Task<RuleExecutionResult> Execute(
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

    private async Task TryPersistResults(T request, PolicyExecutionResult policyExecutionResult)
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

    private async Task<IList<RuleExecutionResult>> Execute(
        Guid executionContextId,
        string correlationId,
        T request,
        CancellationToken cancellationToken)
    {
        List<RuleExecutionResult> ruleExecutionResults = [];

        using Activity? activity = flowRulesEventCounterSource.StartActivity(policy, executionContextId, correlationId);

        foreach (Rule<T> rule in policy.Rules)
        {
            RuleExecutionResult response = await ExecuteRule(rule, executionContextId, correlationId, request, cancellationToken);
            ruleExecutionResults.Add(response);
        }

        return ruleExecutionResults;
    }

    private async Task<RuleExecutionResult> ExecuteRule(
        Rule<T> rule,
        Guid executionContextId,
        string correlationId,
        T request,
        CancellationToken cancellationToken)
    {
        logger.LogExecutionPolicy(rule.Id, rule.Name, executionContextId);

        RuleExecutionResultBuilder ruleExecutionResultBuilder = new(rule.Id, rule.Name, rule.Description);

        long stampStart = TimeProvider.System.GetTimestamp();

        using Activity? activity = flowRulesEventCounterSource.StartActivity(rule, executionContextId, correlationId);

        try
        {
            bool passed = await rule.Source.Invoke(request, cancellationToken);

            if (passed)
            {
                ruleExecutionResultBuilder.WithSuccess();
                flowRulesEventCounterSource.SetSuccess(activity);
            }
            else
            {
                string failureMsg = rule.FailureMessage?.Invoke(request) ?? string.Empty;
                flowRulesEventCounterSource.SetFailure(activity, failureMsg);
                ruleExecutionResultBuilder.WithFailure(failureMsg);
            }
        }
        catch (TaskCanceledException)
        {
            ruleExecutionResultBuilder.WithCancellation();
            flowRulesEventCounterSource.SetFailure(activity, "cancelled");
            logger.LogRuleCancelled(rule.Id, rule.Name);
        }
        catch (Exception ex)
        {
            ruleExecutionResultBuilder.WithException(ex);
            flowRulesEventCounterSource.SetFailure(activity, ex.Message);
            logger.LogExceptionForRule(ex, rule.Id, rule.Name);
        }
        finally
        {
            TimeSpan elapsed = TimeProvider.System.GetElapsedTime(stampStart);
            ruleExecutionResultBuilder.WithTime(elapsed);
            flowRulesEventCounterSource.RuleExecution<T>(policy, rule, executionContextId, correlationId, elapsed);
        }

        return ruleExecutionResultBuilder.ToRuleExecutionResult();
    }
}
