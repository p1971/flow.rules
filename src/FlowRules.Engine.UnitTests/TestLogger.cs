using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FlowRules.Engine.UnitTests
{
    public class TestLogger<T> : ILogger<T>
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestLogger(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            _testOutputHelper.WriteLine($"{typeof(T).Name} {state}");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new DummyDisposable();
        }

        private class DummyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
