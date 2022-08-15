using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine;

/// <inheritdoc />
public class DefaultPolicyResultsRepository<T> : IPolicyResultsRepository<T>
    where T : class
{
    /// <inheritdoc />
    public Task PersistResults(T request, PolicyExecutionResult policyExecutionResult)
    {
        return Task.CompletedTask;
    }
}
