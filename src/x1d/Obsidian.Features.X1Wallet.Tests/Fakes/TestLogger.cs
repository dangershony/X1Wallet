using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Obsidian.Features.X1Wallet.Tests.Fakes
{
    class TestLogger : ILogger
    {
        readonly string categoryName;
        readonly ITestOutputHelper testOutputHelper;

        public TestLogger(string categoryName, ITestOutputHelper testOutputHelper)
        {
            this.categoryName = categoryName;
            this.testOutputHelper = testOutputHelper;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.testOutputHelper.WriteLine($"{logLevel} - {this.categoryName}: {formatter(state, exception)}");
        }
    }
}
