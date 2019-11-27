namespace Obsidian.Features.X1Wallet.NodeUI.Logging
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Extensions.Logging;

    sealed class MyLoggerProvider : ILoggerProvider
    {
        public static readonly ConcurrentQueue<string> Logs = new ConcurrentQueue<string>();
        public static string NodeStats = "";
        public static Action Notify = null;
        static readonly ILogger NullLogger = new NullLogger();

        static int _count;

        static void Add(string line)
        {
            if (line == null)
                return;

            if (_count++ == 0)
            {
                NodeStats = "Starting...";
                return;
            }
            if (line.StartsWith("Information - Stratis.Bitcoin.FullNode: ======Node stats="))
                NodeStats = line.Replace("Information - Stratis.Bitcoin.FullNode: ", "");
            else
            {
                Logs.Enqueue(line);
            }

            Notify?.Invoke();

            _count++;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (Logs == null)
            {
                throw new ObjectDisposedException($"{nameof(MyLoggerProvider)} has already been disposed.");
            }

            if (categoryName.StartsWith("Microsoft"))
            {
                return NullLogger;
            }
            return new MyLogger(categoryName, Add);
        }

        public void Dispose()
        {

        }
    }
}
