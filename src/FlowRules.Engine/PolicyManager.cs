using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Extensions.Logging;

namespace FlowRules.Engine
{
    /// <inheritdoc />
    public class PolicyManager<T> : IPolicyManager<T>
        where T : class
    {
        private readonly Policy<T> _policy;
        private readonly IPolicyResultsRepository<T> _resultsRepository;
        private readonly ILogger<PolicyManager<T>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyManager{T}"/> class.
        /// </summary>
        /// <param name="policy">The policy to execute.</param>
        /// <param name="resultsRepository">The result repository to write results to.</param>
        /// <param name="logger">The logger to use.</param>
        public PolicyManager(Policy<T> policy, IPolicyResultsRepository<T> resultsRepository, ILogger<PolicyManager<T>> logger)
        {
            _policy = policy;
            _resultsRepository = resultsRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<PolicyExecutionResult> Execute(
            string correlationId,
            Guid executionContextId,
            T request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Executing [{policyId}]:[{policyName}] for [{executionContextId}]",
                _policy.Id,
                _policy.Name,
                executionContextId);

            Stopwatch stopwatch = Stopwatch.StartNew();

            IList<RuleExecutionResult> response = await Execute(_policy, executionContextId, request, cancellationToken);

            PolicyExecutionResult policyExecutionResult =
                new()
                {
                    RuleContextId = executionContextId,
                    CorrelationId = correlationId,
                    PolicyId = _policy.Id,
                    PolicyName = _policy.Name,
                    Version = _policy.GetType().Assembly.GetName().Version?.ToString(4),
                    RuleExecutionResults = response.ToArray(),
                    Passed = response.All(r => r.Passed)
                };

            stopwatch.Stop();

#pragma warning disable CS4014
            Task.Run(() => TryPersistResults(request, policyExecutionResult), cancellationToken);
#pragma warning restore CS4014

            FlowRulesEventCounterSource.EventSource.PolicyExecution(_policy.Id, stopwatch.ElapsedMilliseconds);

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
            _logger.LogInformation(
                "Executing [{ruleId}] for [{executionContextId}]",
                ruleId,
                executionContextId);

            Rule<T> rule = _policy.Rules.FirstOrDefault(r => r.Id == ruleId);

            if (rule == null)
            {
                throw new InvalidOperationException($"No rule with id [{ruleId}] was found.");
            }

            return await ExecuteRule(rule, executionContextId, request, cancellationToken);
        }

        private async Task TryPersistResults(T request, PolicyExecutionResult policyExecutionResult)
        {
            try
            {
                await _resultsRepository.PersistResults(request, policyExecutionResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An exception occurred writing the results to the [{repositoryTypeName}] for [{ruleContextId}]",
                    _resultsRepository.GetType().Name,
                    policyExecutionResult.RuleContextId);
            }
        }

        private async Task<IList<RuleExecutionResult>> Execute(
            Policy<T> policy,
            Guid executionContextId,
            T request,
            CancellationToken cancellationToken)
        {
            IList<RuleExecutionResult> ruleExecutionResults = new List<RuleExecutionResult>();
            foreach (Rule<T> rule in policy.Rules)
            {
                RuleExecutionResult response = await ExecuteRule(rule, executionContextId, request, cancellationToken);
                ruleExecutionResults.Add(response);
            }

            return ruleExecutionResults;
        }

        private async Task<RuleExecutionResult> ExecuteRule(
            Rule<T> rule,
            Guid executionContextId,
            T request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("... executing [{policyId}]:[{policyName}] for [{executionContextId}]", rule.Id, rule.Name, executionContextId);

            RuleExecutionResult result = new(rule.Id, rule.Name, rule.Description);

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                bool passed = await rule.Source.Invoke(request, cancellationToken);
                result.Passed = passed;
                if (!passed && rule.FailureMessage != null)
                {
                    result.Message = rule.FailureMessage(request);
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Exception = ex;
                result.Message = ex.Message;
                _logger.LogError(ex, "An exception occurred executing [{ruleId}]:[{ruleName}]", rule.Id, rule.Name);
            }
            finally
            {
                stopwatch.Stop();
                result.Elapsed = stopwatch.Elapsed;
                FlowRulesEventCounterSource.EventSource.RuleExecution(_policy.Id, rule.Id, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
    }
}
