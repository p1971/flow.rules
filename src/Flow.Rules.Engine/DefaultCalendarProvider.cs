using System;
using Flow.Rules.Engine.Interfaces;

namespace Flow.Rules.Engine
{
    public class DefaultCalendarProvider : ICalendarProvider
    {
        public DateTime CurrentDateTime => DateTime.UtcNow;
    }
}
