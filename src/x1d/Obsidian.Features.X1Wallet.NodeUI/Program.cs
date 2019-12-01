using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Protocol;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.NodeUI.Logging;
using Obsidian.Features.X1Wallet.SecureApi;
using Obsidian.Networks.ObsidianX;
using Obsidian.x1d.Api;
using Obsidian.x1d.Util;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Utilities;

namespace Obsidian.Features.X1Wallet.NodeUI
{
    static class Program
    {
        static CancellationTokenSource cancellationTokenSource;

        public static event EventHandler Started;

        public static event EventHandler Stopped;

        public static IFullNode FullNode { get; private set; }

        public static void Start(string[] args)
        {
            var nodeTask = MainAsync(args);
            Started?.Invoke(null, EventArgs.Empty);
            nodeTask.Wait();
            cancellationTokenSource.Cancel();
            FullNode = null;
            Stopped?.Invoke(null, EventArgs.Empty);
        }

        static async Task MainAsync(string[] args)
        {
            try
            {
                var nodeSettings = GetPatchedNodeSettings(args);

                var builder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseX1Wallet()
                    .UseX1WalletApi()
                    .UseSecureApiHost();

                FullNode = builder.Build();

                Init.PrintWelcomeMessage(nodeSettings, FullNode);

                cancellationTokenSource = Init.RunIfDebugModeDelayed(FullNode);

                await FullNode.RunAsync();
            }
            catch (Exception e)
            {
                var message = $"Critical error in {Init.GetName()}: {e.Message}";
                try
                {
                    FullNode.NodeService<ILoggerFactory>().CreateLogger(typeof(Program).Name).LogCritical(message);
                }
                catch (Exception)
                {
                    Console.WriteLine(message);
                }
            }
        }

        static NodeSettings GetPatchedNodeSettings(string[] args)
        {
            var nodeSettings = new UiNodeSettings(networksSelector: ObsidianXNetworksSelector.Obsidian,
                protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, agent: $"{Init.GetName()}", args: Init.MergeArgs(args))
            {
                MinProtocolVersion = ProtocolVersion.PROVEN_HEADER_VERSION
            };
            nodeSettings.InitLoggerFactory();
            return nodeSettings;
        }
    }
}
