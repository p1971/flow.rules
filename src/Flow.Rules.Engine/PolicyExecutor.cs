using System;
using System.Collections.Generic;
using Flow.Rules.Engine.Interfaces;
using Flow.Rules.Engine.Models;

namespace Flow.Rules.Engine
{
    public class PolicyExecutor : IPolicyExecutor
    {
        private readonly ICalendarProvider _calendarProvider;

        public PolicyExecutor(ICalendarProvider calendarProvider)
        {
            _calendarProvider = calendarProvider;
        }

        public IList<RuleExecutionResult> Execute<T>(Policy<T> policy, T request, Lookups lookups) where T : class
        {
            IList<RuleExecutionResult> ruleExecutionResults = new List<RuleExecutionResult>();
            foreach (Rule<T> rule in policy.Rules)
            {
                RuleExecutionResult response = Execute(rule, request, lookups);
                ruleExecutionResults.Add(response);
            }
            return ruleExecutionResults;
        }

        private RuleExecutionResult Execute<T>(Rule<T> rule, T request, Lookups lookups)
            where T : class
        {
            RuleExecutionResult result = new RuleExecutionResult(rule.Id, rule.Name, rule.Description);

            try
            {
                bool passed = rule.Source.Invoke(request, lookups, _calendarProvider);
                result.Passed = passed;
                if (!passed)
                {
                    result.Message = rule.FailureMessage(request);
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Message = ex.Message;
            }

            return result;
        }
    }
}
