using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

namespace FlowRules.Engine;

/// <summary>
/// A type-erased registry of policies across multiple request types.
/// Dispatches execution to the correct <see cref="IPolicyManager{T}"/> by policy id.
/// </summary>
internal sealed class PolicyRegistry : IPolicyRegistry
{
    private readonly Dictionary<string, IPolicyRegistryEntry> _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyRegistry"/> class.
    /// </summary>
    /// <param name="entries">All registered policy entries, supplied by the DI container.</param>
    /// <exception cref="InvalidOperationException">Thrown when two policies share the same id.</exception>
    public PolicyRegistry(IEnumerable<IPolicyRegistryEntry> entries)
    {
        _entries = [];

        foreach (IPolicyRegistryEntry entry in entries)
        {
            if (!_entries.TryAdd(entry.PolicyId, entry))
            {
                throw new InvalidOperationException(
                    $"A policy with id '{entry.PolicyId}' has already been registered. Policy ids must be unique across the registry.");
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> PolicyIds => [.. _entries.Keys];

    /// <inheritdoc />
    public ValueTask<PolicyExecutionResult> ExecuteAsync<T>(
        string policyId,
        string correlationId,
        Guid executionContextId,
        T request,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!_entries.TryGetValue(policyId, out IPolicyRegistryEntry? entry))
        {
            throw new InvalidOperationException(
                $"No policy with id '{policyId}' has been registered. Registered policies: [{string.Join(", ", _entries.Keys)}].");
        }

        if (entry is not IPolicyRegistryEntry<T> typedEntry)
        {
            throw new InvalidOperationException(
                $"Policy '{policyId}' operates on '{entry.RequestType.Name}', not '{typeof(T).Name}'.");
        }

        return typedEntry.ExecuteAsync(correlationId, executionContextId, request, cancellationToken);
    }
}
