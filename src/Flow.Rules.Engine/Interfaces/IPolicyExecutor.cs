using System.Collections.Generic;
using Flow.Rules.Engine.Models;

namespace Flow.Rules.Engine.Interfaces
{
    public interface IPolicyExecutor
    {
        IList<RuleExecutionResult> Execute<T>(Policy<T> policy, T request, Lookups lookups)
            where T : class;
    }
}
