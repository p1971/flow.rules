using System;
using FlowRules.Engine.UnitTests.Interfaces;

namespace FlowRules.Engine.UnitTests
{
    public class DefaultCalendarProvider : ICalendarProvider
    {
        public DateTime CurrentDateTime => DateTime.UtcNow;
    }
}
