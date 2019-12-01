using Microsoft.Extensions.Logging.Console;

namespace Obsidian.Features.X1Wallet.NodeUI.Logging
{
    using System;
    using System.Diagnostics;
    using Microsoft.Extensions.Logging;

    internal sealed class MyLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerProvider loggerProvider;
        public MyLoggerFactory()
        {
            this.loggerProvider = new MyLoggerProvider();
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
