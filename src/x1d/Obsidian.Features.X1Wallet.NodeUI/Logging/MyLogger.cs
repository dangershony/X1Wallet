namespace Obsidian.Features.X1Wallet.NodeUI.Logging
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Microsoft.Extensions.Logging;

    internal sealed class MyLogger : ILogger
    {
        private readonly string categoryName;
        private readonly Action<string> add;

        public MyLogger(string categoryName, Action<string> add)
        {
            this.categoryName = categoryName;
            this.add = add;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if ((int) logLevel < 2)
                return false;
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var line = $"{logLevel} - {this.categoryName}: {formatter(state, exception)}";
            this.add(line);
            Debug.WriteLine(line);
        }
    }
}
