using System;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FlowRules.Engine.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for the FlowRules.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the FlowRules dependencies to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// Multiple policies for the same <typeparamref name="T"/> are supported.
    /// Each policy and its manager are keyed by <see cref="Policy{T}.Id"/> so they
    /// coexist independently in the container. Direct resolution of
    /// <see cref="IPolicyManager{T}"/> (unkeyed) returns the last-registered policy for
    /// that type; prefer <see cref="IPolicyRegistry"/> for id-based dispatch.
    /// <see cref="IPolicyResultsRepository{T}"/> is registered once per request type,
    /// not per policy id, so policies for the same <typeparamref name="T"/> share the
    /// same results repository implementation.
    /// </remarks>
    /// <typeparam name="T">The type that the rules will execute against.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="policyAction">A function to return the policy.</param>
    /// <param name="setupAction">A setup function for the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddFlowRules<T>(
        this IServiceCollection services,
        Func<Policy<T>> policyAction,
        Action<FlowRulesOptions>? setupAction = null)
        where T : class
    {
        FlowRulesOptions options = new();

        setupAction?.Invoke(options);

        Policy<T> policy = policyAction();
        string policyId = policy.Id;

        // Key the Policy<T> and its IPolicyManager<T> by policy id so that multiple
        // policies for the same T can coexist without the second registration
        // silently overwriting the first.
        services.AddKeyedSingleton<Policy<T>>(policyId, (_, _) => policy);

        if (options.ResultsRepository != null)
        {
            services.TryAddSingleton(typeof(IPolicyResultsRepository<T>), options.ResultsRepository);
        }
        else
        {
            services.TryAddSingleton<IPolicyResultsRepository<T>, DefaultPolicyResultsRepository<T>>();
        }

        if (options.ExportTelemetry)
        {
            services.TryAddSingleton<IFlowRulesTelemetryService, FlowRulesTelemetryService>();
        }
        else
        {
            services.Replace(ServiceDescriptor.Singleton<IFlowRulesTelemetryService, NoOpFlowRulesTelemetryService>());
        }

        services.AddKeyedSingleton<IPolicyManager<T>>(policyId, (sp, _) =>
        {
            Policy<T> resolvedPolicy             = sp.GetRequiredKeyedService<Policy<T>>(policyId);
            IPolicyResultsRepository<T> repo     = sp.GetRequiredService<IPolicyResultsRepository<T>>();
            IFlowRulesTelemetryService telemetry = sp.GetRequiredService<IFlowRulesTelemetryService>();
            ILogger<PolicyManager<T>> logger     = sp.GetRequiredService<ILogger<PolicyManager<T>>>();
            return new PolicyManager<T>(resolvedPolicy, repo, telemetry, logger);
        });

        // Also register an unkeyed IPolicyManager<T> so existing code that injects
        // IPolicyManager<T> directly continues to work (last registration wins).
        services.AddSingleton<IPolicyManager<T>>(sp =>
            sp.GetRequiredKeyedService<IPolicyManager<T>>(policyId));

        // Register a typed, type-erased entry so AddFlowRulesRegistry() can collect
        // all policies and dispatch by id across different DTO types.
        services.AddSingleton<IPolicyRegistryEntry>(sp =>
        {
            IPolicyManager<T> manager = sp.GetRequiredKeyedService<IPolicyManager<T>>(policyId);
            return new PolicyRegistryEntry<T>(policyId, manager);
        });

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IPolicyRegistry"/> that aggregates all policies added via
    /// <see cref="AddFlowRules{T}"/> and allows dispatch by policy id.
    /// Call this once after all <c>AddFlowRules</c> registrations.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddFlowRulesRegistry(this IServiceCollection services)
    {
        services.AddSingleton<IPolicyRegistry, PolicyRegistry>();
        return services;
    }
}
