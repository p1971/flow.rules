using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine;

internal class NullPolicyResultsRepository<T> : IPolicyResultsRepository<T>
    where T : class
{
    public Task PersistResults(T request, PolicyExecutionResult result)
    {
        return Task.CompletedTask;
    }
}
