using System.Threading.Tasks;
using FlowRules.Engine.Models;

namespace FlowRules.Engine.Interfaces;

/// <summary>
/// Interface used to persist the policy results to some store.
/// </summary>
/// <typeparam name="T">The type the policy was written for.</typeparam>
public interface IPolicyResultsRepository<in T>
    where T : class
{
    /// <summary>
    /// Writes the <see cref="PolicyExecutionResult"/> to a persistence store.
    /// </summary>
    /// <param name="request">The request model.</param>
    /// <param name="policyExecutionResult">The results of running the policy.</param>
    /// <returns>A task.</returns>
    Task PersistResults(T request, PolicyExecutionResult policyExecutionResult);
}
