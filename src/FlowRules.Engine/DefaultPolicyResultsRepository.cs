using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine;

public class DefaultPolicyResultsRepository<T> : IPolicyResultsRepository<T>
    where T : class
{
    public Task PersistResults(T request, PolicyExecutionResult result)
    {
        return Task.CompletedTask;
    }
}
