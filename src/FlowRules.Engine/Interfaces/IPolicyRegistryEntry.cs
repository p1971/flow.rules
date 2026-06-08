using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Models;

namespace FlowRules.Engine.Interfaces;

/// <summary>
/// Base interface for policy registry entries, providing policy identity and type metadata.
/// Enables <see cref="IPolicyRegistry"/> to hold a heterogeneous collection of policies.
/// </summary>
internal interface IPolicyRegistryEntry
{
    /// <summary>
    /// Gets the id of the policy this entry represents.
    /// </summary>
    string PolicyId { get; }

    /// <summary>
    /// Gets the CLR type of the request DTO this policy operates on.
    /// </summary>
    Type RequestType { get; }
}

/// <summary>
/// Typed policy registry entry for policies operating on request type <typeparamref name="T"/>.
/// Provides compile-time type safety for execution without requiring <see cref="object"/> casts.
/// </summary>
/// <typeparam name="T">The request type the policy operates on.</typeparam>
internal interface IPolicyRegistryEntry<T> : IPolicyRegistryEntry
    where T : class
{
    /// <summary>
    /// Executes the policy against the typed request.
    /// </summary>
    ValueTask<PolicyExecutionResult> ExecuteAsync(
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken);
}
