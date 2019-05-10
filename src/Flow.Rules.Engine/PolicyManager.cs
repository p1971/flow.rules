using System;
using System.Linq;
using Flow.Rules.Engine.Interfaces;
using Flow.Rules.Engine.Models;

namespace Flow.Rules.Engine
{
    public class PolicyManager<T> : IPolicyManager<T>
        where T : class
    {
        private readonly Policy<T> _policy;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly Lookups _lookups;

        public PolicyManager(Policy<T> policy, IPolicyExecutor policyExecutor, ILookupProvider lookupProvider)
        {
            _policy = policy;
            _policyExecutor = policyExecutor;
            _lookups = lookupProvider.GetLookups();
        }

        public PolicyExecutionResult Execute(Guid ruleContextId, string policyName, T request)
        {
            PolicyExecutionResult policyExecutionResult =
                new PolicyExecutionResult
                {
                    RuleContextId = ruleContextId,
                    PolicyId = _policy.Id,
                    PolicyName = _policy.Name,
                    RuleExecutionResults = _policyExecutor.Execute(_policy, request, _lookups).ToArray()
                };

            policyExecutionResult.Passed = policyExecutionResult.RuleExecutionResults.All(r => r.Passed);

            return policyExecutionResult;
        }
    }
}
