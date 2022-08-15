using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Models;

namespace FlowRules.Engine.Interfaces
{
    /// <summary>
    /// Interface for manipulating policies.
    /// </summary>
    /// <typeparam name="T">The request type.</typeparam>
    public interface IPolicyManager<in T>
        where T : class
    {
        /// <summary>
        /// Executes the policy for the given request.
        /// </summary>
        /// <param name="executionContextId">An execution context id.</param>
        /// <param name="request">The request model to apply rules to.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task containing the <see cref="PolicyExecutionResult"/>.</returns>
        Task<PolicyExecutionResult> Execute(Guid executionContextId, T request, CancellationToken cancellationToken);
    }
}
