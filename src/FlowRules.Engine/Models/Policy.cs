using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowRules.Engine.Models;

/// <summary>
/// Represents a rules policy.
/// </summary>
/// <typeparam name="T">The type the policy is to be executed against.</typeparam>
public class Policy<T>(string id, string name, string? description, IList<Rule<T>> rules, string? version = null)
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
    /// Gets the version of the policy.
    /// </summary>
    public string? Version { get; } = version;

    /// <summary>
    /// Gets the rules of the policy.
    /// </summary>
    public IReadOnlyList<Rule<T>> Rules { get; } = ValidateRules(rules);

    private static string ValidateId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        return id;
    }

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return name;
    }

    private static IReadOnlyList<Rule<T>> ValidateRules(IList<Rule<T>> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (rules.Count == 0)
        {
            throw new ArgumentException("A policy must contain at least one rule.", nameof(rules));
        }

        List<Rule<T>> rulesSnapshot = [.. rules];

        Rule<T>? duplicateRule = rulesSnapshot
            .GroupBy(rule => rule.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?
            .First();

        if (duplicateRule != null)
        {
            throw new ArgumentException($"A policy cannot contain duplicate rule ids. Duplicate id: [{duplicateRule.Id}].", nameof(rules));
        }

        return rulesSnapshot.AsReadOnly();
    }
}
