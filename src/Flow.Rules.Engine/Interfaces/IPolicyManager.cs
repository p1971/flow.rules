using System;
using Flow.Rules.Engine.Models;

namespace Flow.Rules.Engine.Interfaces
{
    public interface IPolicyManager<in T>
        where T : class
    {
        PolicyExecutionResult Execute(Guid ruleContextId, string policyName, T request);
    }
}
