using System.Linq;
using Microsoft.Extensions.Logging;
using Obsidian.Features.X1Wallet.Models.Wallet;

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

        internal static void BlockAddedToWallet(int height, BlockMetadata blockMetadata)
        {
            Logger.LogInformation(
                $"Block {height} added {blockMetadata.Transactions.Count} {blockMetadata.Transactions.Sum(x => x.ValueAdded) / C.SatoshisPerCoin} {C.Network.CoinTicker} to the wallet.");
        }
    }
}
