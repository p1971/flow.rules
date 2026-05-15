using System;
using System.Collections.Generic;

namespace FlowRules.Engine.Models;

/// <summary>
/// Represents a rules policy.
/// </summary>
/// <typeparam name="T">The type the policy is to be executed against.</typeparam>
public class Policy<T>(string id, string name, string? description, IList<Rule<T>> rules)
    where T : class
{
    /// <summary>
    /// Gets the id of the policy.
    /// </summary>
    public string Id { get; } = ValidateId(id);

    /// <summary>
    /// Gets the name of the policy.
    /// </summary>
    public string Name { get; } = ValidateName(name);

    /// <summary>
    /// Gets the description of the policy.
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    /// Gets the rules of the policy.
    /// </summary>
    public IReadOnlyList<Rule<T>> Rules { get; } = ValidateRules(rules);

    private static string ValidateId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(id));
        return value;
    }

    private static string ValidateName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(name));
        return value;
    }

    private static IReadOnlyList<Rule<T>> ValidateRules(IList<Rule<T>> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Count == 0)
        {
            throw new ArgumentException("A policy must contain at least one rule.", nameof(rules));
        }

        return value.AsReadOnly();
    }
}
