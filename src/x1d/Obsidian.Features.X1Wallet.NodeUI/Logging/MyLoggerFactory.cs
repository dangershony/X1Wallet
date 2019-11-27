using Microsoft.Extensions.Logging.Console;

namespace Obsidian.Features.X1Wallet.NodeUI.Logging
{
    using System;
    using System.Diagnostics;
    using Microsoft.Extensions.Logging;
    using Stratis.Bitcoin.Configuration.Logging;

    internal sealed class MyLoggerFactory : ExtendedLoggerFactory, ILoggerFactory
    {
        private readonly ILoggerProvider loggerProvider;
        public MyLoggerFactory()
        {
            this.loggerProvider = new MyLoggerProvider();
            this.ConsoleLoggerProvider = this.loggerProvider;
        }

        public new void AddProvider(ILoggerProvider provider)
        {
            if (provider != null)
            {
                Debug.WriteLine($"ILoggerProvider {provider.GetType()} is not supported.");
            }
        }

        public new ILogger CreateLogger(string categoryName)
        {
            return this.loggerProvider.CreateLogger(categoryName);
        }

        public new void Dispose()
        {
            this.loggerProvider.Dispose();
        }
    }
}
