using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine;

/// <inheritdoc />
internal class DefaultPolicyResultsRepository<T> : IPolicyResultsRepository<T>
    where T : class
{
    /// <inheritdoc />
    public ValueTask PersistResults(T request, PolicyExecutionResult policyExecutionResult)
    {
        return ValueTask.CompletedTask;
    }
}
