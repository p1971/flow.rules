using System;

namespace FlowRules.Engine.UnitTests.Interfaces
{
    public interface ICalendarProvider
    {
        DateTime CurrentDateTime { get; }
    }
}
