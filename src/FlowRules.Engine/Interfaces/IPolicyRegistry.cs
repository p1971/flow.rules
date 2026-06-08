using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Models;

namespace FlowRules.Engine.Interfaces;

/// <summary>
/// A registry of policies across multiple request types.
/// Allows client workflows to execute any registered policy by its id without
/// requiring a separately injected <see cref="IPolicyManager{T}"/> per policy.
/// </summary>
public interface IPolicyRegistry
{
    /// <summary>
    /// Gets the ids of all policies registered in this registry.
    /// </summary>
    IReadOnlyList<string> PolicyIds { get; }

    /// <summary>
    /// Executes the policy identified by <paramref name="policyId"/> using a typed request.
    /// Provides compile-time type safety — no <see cref="object"/> cast required.
    /// </summary>
    /// <typeparam name="T">The request type the policy operates on.</typeparam>
    /// <param name="policyId">The id of the policy to execute.</param>
    /// <param name="correlationId">A correlation id for the execution.</param>
    /// <param name="executionContextId">A unique id for this execution context.</param>
    /// <param name="request">The typed request object.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="PolicyExecutionResult"/> for the executed policy.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="policyId"/> is not registered, or when the registered
    /// policy does not operate on <typeparamref name="T"/>.
    /// </exception>
    ValueTask<PolicyExecutionResult> ExecuteAsync<T>(
        string policyId,
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
        where T : class;
}
