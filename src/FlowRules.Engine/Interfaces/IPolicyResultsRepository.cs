using System.Threading.Tasks;
using FlowRules.Engine.Models;

namespace FlowRules.Engine.Interfaces;

public interface IPolicyResultsRepository<in T>
    where T : class
{
    Task PersistResults(T request, PolicyExecutionResult result);
}
