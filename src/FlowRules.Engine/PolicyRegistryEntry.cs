using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine;

/// <summary>
/// Wraps a typed <see cref="IPolicyManager{T}"/> as an <see cref="IPolicyRegistryEntry{T}"/>
/// so that the registry can hold managers for different request types in one collection.
/// </summary>
/// <typeparam name="T">The request type the policy operates on.</typeparam>
internal sealed class PolicyRegistryEntry<T>(string policyId, IPolicyManager<T> manager)
    : IPolicyRegistryEntry<T>
    where T : class
{
    /// <inheritdoc />
    public string PolicyId { get; } = policyId;

    /// <inheritdoc />
    public Type RequestType { get; } = typeof(T);

    /// <inheritdoc />
    public ValueTask<PolicyExecutionResult> ExecuteAsync(
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken) =>
        manager.Execute(correlationId, executionContextId, request, cancellationToken);
}
