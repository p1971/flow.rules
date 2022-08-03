using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine
{
    public class PolicyManager<T> : IPolicyManager<T>
        where T : class
    {
        private readonly Policy<T> _policy;
        private readonly IPolicyResultsRepository<T> _resultsRepository;

        public PolicyManager(Policy<T> policy, IPolicyResultsRepository<T> resultsRepository)
        {
            _policy = policy;
            _resultsRepository = resultsRepository;
        }

        public async Task<PolicyExecutionResult> Execute(Guid executionContextId, T request, CancellationToken cancellationToken)
        {
            IList<RuleExecutionResult> response = await Execute(_policy, request, cancellationToken);

            PolicyExecutionResult policyExecutionResult =
                new()
                {
                    RuleContextId = executionContextId,
                    PolicyId = _policy.Id,
                    PolicyName = _policy.Name,
                    Version = _policy.GetType().Assembly.GetName().Version?.ToString(4),
                    RuleExecutionResults = response.ToArray()
                };

            policyExecutionResult.Passed = policyExecutionResult.RuleExecutionResults.All(r => r.Passed);

            await _resultsRepository.PersistResults(request, policyExecutionResult);

            return policyExecutionResult;
        }

        private static async Task<IList<RuleExecutionResult>> Execute(
            Policy<T> policy,
            T request,
            CancellationToken cancellationToken)
        {
            IList<RuleExecutionResult> ruleExecutionResults = new List<RuleExecutionResult>();
            foreach (Rule<T> rule in policy.Rules)
            {
                RuleExecutionResult response = await ExecuteRule(rule, request, cancellationToken);
                ruleExecutionResults.Add(response);
            }
            return ruleExecutionResults;
        }

        private static async Task<RuleExecutionResult> ExecuteRule(Rule<T> rule, T request, CancellationToken cancellationToken)
        {
            RuleExecutionResult result = new(rule.Id, rule.Name, rule.Description);

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                bool passed = await rule.Source.Invoke(request, cancellationToken);
                result.Passed = passed;
                if (!passed)
                {
                    result.Message = rule.FailureMessage(request);
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Exception = ex;
                result.Message = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.Elapsed = stopwatch.Elapsed;
            }

            return result;
        }
    }
}
