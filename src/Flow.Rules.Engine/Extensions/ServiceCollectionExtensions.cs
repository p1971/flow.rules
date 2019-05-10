using System;
using Flow.Rules.Engine.Interfaces;
using Flow.Rules.Engine.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.Rules.Engine.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFlowRules<T>(
            this IServiceCollection services,
            Func<Policy<T>> ruleAction,
            Action<FlowRulesOptions> setupAction = null)
            where T : class
        {
            FlowRulesOptions options = new FlowRulesOptions();

            setupAction?.Invoke(options);

            services.AddSingleton<ILookupProvider>(new LookupProvider(options.Lookups));
            services.AddSingleton<IPolicyExecutor, PolicyExecutor>();
            services.AddSingleton<ICalendarProvider, DefaultCalendarProvider>();

            Policy<T> policy = ruleAction();
            services.AddSingleton(policy);

            services.AddSingleton<IPolicyManager<T>, PolicyManager<T>>();

            return services;
        }
    }
}
