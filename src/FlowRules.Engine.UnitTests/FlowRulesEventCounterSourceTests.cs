using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace FlowRules.Engine.UnitTests
{
    public class FlowRulesEventCounterSourceTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly TestEventListener _testEventListener = new();
        private readonly FlowRulesEventCounterSource _subject = FlowRulesEventCounterSource.EventSource;

        public FlowRulesEventCounterSourceTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task FlowRulesEventCounterSource_PolicyExecution_Should_EmitEvent()
        {
            _testOutputHelper.WriteLine($"{nameof(FlowRulesEventCounterSource_PolicyExecution_Should_EmitEvent)} - writing events.");

            Assert.True(_subject.IsEnabled());

            _subject.PolicyExecution("P001", 100);
            _subject.PolicyExecution("P001", 200);
            _subject.RuleExecution("P001", "R001", 40);
            _subject.RuleExecution("P001", "R002", 50);

            await Task.Delay(2000);

            if (_testEventListener.EventArgs.Count == 0)
            {
                Assert.Fail("No events were found.");
            }

            (string key, string value) policyEvents = _testEventListener.EventArgs.FirstOrDefault(a => a.key == "P001");
            Assert.NotNull(policyEvents.key);

            (string key, string value) ruleEvents = _testEventListener.EventArgs.FirstOrDefault(a => a.key == "P001:R001");
            Assert.NotNull(ruleEvents.key);

            _testEventListener.DisableEvents(new EventSource(FlowRulesEventCounterSource.EventSourceName));
        }
        
        private class TestEventListener : EventListener
        {
            public IList<(string key, string value)> EventArgs { get; set; } = new List<(string, string)>();

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == FlowRulesEventCounterSource.EventSourceName)
                {
                    EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>()
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });
                }
            }
            
            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.Payload != null)
                {
                    for (int i = 0; i < eventData.Payload.Count; ++i)
                    {
                        if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                        {
                            var (counterName, counterValue) = GetRelevantMetric(eventPayload);

                            EventArgs.Add(new(counterName, counterValue));
                        }
                    }
                }
            }

            private static (string counterName, string counterValue) GetRelevantMetric(
                IDictionary<string, object> eventPayload)
            {
                var counterName = "";
                var counterValue = "";

                if (eventPayload.TryGetValue("DisplayName", out object displayValue))
                {
                    counterName = displayValue.ToString();
                }
                if (eventPayload.TryGetValue("Mean", out object value) ||
                    eventPayload.TryGetValue("Increment", out value))
                {
                    counterValue = value.ToString();
                }

                return (counterName, counterValue);
            }
        }
    }
}
