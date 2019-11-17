using System;
using Microsoft.Extensions.Logging;

namespace Obsidian.Features.X1Wallet.Tools
{
    /// <summary>
    /// Static logger for logging also from static methods.
    /// </summary>
    static class Log
    {
        public static ILogger Logger;

        public static void SetLogger(ILogger logger)
        {
            Logger = logger;
        }
    }
}
