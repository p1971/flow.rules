using System;

namespace Flow.Rules.Engine.Interfaces
{
    public interface ICalendarProvider
    {
        DateTime CurrentDateTime { get; }
    }
}
