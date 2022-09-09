using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace FlowRules.Engine
{
    /// <summary>
    /// Provides dotnet counters for the FlowRules.
    /// </summary>
    [EventSource(Name = FlowRulesEventCounterSource.EventSourceName)]
    public sealed class FlowRulesEventCounterSource : EventSource
    {
        /// <summary>
        /// The name of the event source.
        /// </summary>
        public const string EventSourceName = "FlowRules";

        /// <summary>
        /// Static instance of the <see cref="FlowRulesEventCounterSource"/>.
        /// </summary>
        public static readonly FlowRulesEventCounterSource EventSource = new();

        private readonly ConcurrentDictionary<string, EventCounter> _counters = new();

        private FlowRulesEventCounterSource()
            : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        /// <summary>
        /// Writes and event counter to indicate a policy was executed.
        /// </summary>
        /// <param name="policyId">The id of the policy.</param>
        /// <param name="elapsedMilliseconds">The time taken to execute the policy in milliseconds.</param>
        [Event(1, Level = EventLevel.Informational)]
        public void PolicyExecution(string policyId, long elapsedMilliseconds)
        {
            if (IsEnabled())
            {
                string key = $"{policyId}";
                EventCounter counter = _counters.GetOrAdd(key, (key) =>
                    new EventCounter(key, this)
                    {
                        DisplayName = $"{key}",
                        DisplayUnits = "ms"
                    });

                counter.WriteMetric(elapsedMilliseconds);
            }
        }

        /// <summary>
        /// Writes and event counter to indicate how long the policy took to execute.
        /// </summary>
        /// <param name="policyId">The id of the policy.</param>
        /// <param name="ruleId">The id of the rule.</param>
        /// <param name="elapsedMilliseconds">The time taken to execute the policy in milliseconds.</param>
        [Event(2, Level = EventLevel.Informational)]
        public void RuleExecution(string policyId, string ruleId, long elapsedMilliseconds)
        {
            if (IsEnabled())
            {
                string key = $"{policyId}:{ruleId}";
                EventCounter counter = _counters.GetOrAdd(key, (key) =>
                    new EventCounter(key, this)
                    {
                        DisplayName = $"{key}",
                        DisplayUnits = "ms"
                    });

                counter.WriteMetric(elapsedMilliseconds);
            }
        }
    }
}
