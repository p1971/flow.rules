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
        private readonly TestEventListenerCollector _collector = new();
        private readonly TestEventListener _testEventListener;

        public FlowRulesEventCounterSourceTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _testEventListener = new TestEventListener(testOutputHelper, _collector);
        }

        [Fact]
        public async Task FlowRulesEventCounterSource_PolicyExecution_Should_EmitEvent()
        {
            _testOutputHelper.WriteLine($"{nameof(FlowRulesEventCounterSource_PolicyExecution_Should_EmitEvent)} - writing event.");

            FlowRulesEventCounterSource subject = FlowRulesEventCounterSource.EventSource;

            subject.PolicyExecution("P001", 100);
            subject.PolicyExecution("P001", 200);
            subject.RuleExecution("P001", "R001", 40);
            subject.RuleExecution("P001", "R002", 50);

            await Task.Delay(2000);

            if (_collector.EventArgs.Count == 0)
            {
                Assert.Fail("No events were found.");
            }

            (string key, string value) policyEvents = _collector.EventArgs.FirstOrDefault(a => a.key == "P001");
            Assert.NotNull(policyEvents.key);

            (string key, string value) ruleEvents = _collector.EventArgs.FirstOrDefault(a => a.key == "P001:R001");
            Assert.NotNull(ruleEvents.key);

            _testEventListener.DisableEvents(new EventSource(FlowRulesEventCounterSource.EventSourceName));
        }

        private class TestEventListenerCollector
        {
            public void Add(string key, string value)
            {
                EventArgs.Add((key, value));
            }

            public IList<(string key, string value)> EventArgs { get; set; } = new List<(string, string)>();
        }

        private class TestEventListener : EventListener
        {
            private readonly ITestOutputHelper _testOutputHelper;
            private readonly TestEventListenerCollector _eventListenerCollector;

            public TestEventListener(ITestOutputHelper testOutputHelper, TestEventListenerCollector eventListenerCollector)
            {
                _testOutputHelper = testOutputHelper;
                _eventListenerCollector = eventListenerCollector;
                _testOutputHelper.WriteLine($"Created {nameof(TestEventListener)}");
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == FlowRulesEventCounterSource.EventSourceName)
                {
                    _testOutputHelper.WriteLine($"Listening to event source: [{eventSource.Name}]");
                    EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>()
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                _testOutputHelper.WriteLine($"{eventData.EventSource.Name}");

                for (int i = 0; i < eventData.Payload.Count; ++i)
                {
                    if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                    {
                        var (counterName, counterValue) = GetRelevantMetric(eventPayload);
                        _eventListenerCollector.Add(counterName, counterValue);

                        _testOutputHelper.WriteLine($"{counterName} : {counterValue}");
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
