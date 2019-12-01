using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Protocol;
using Obsidian.Features.X1Wallet.Tools;
using Obsidian.Networks.ObsidianX;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Configuration;

namespace Obsidian.x1d.Util
{
    public static class Init
    {
        internal static NodeSettings GetNodeSettings(string[] args)
        {
            var nodeSettings = new NodeSettings(networksSelector: ObsidianXNetworksSelector.Obsidian,
                protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, agent: $"{GetName()}", args: MergeArgs(args))
            {
                MinProtocolVersion = ProtocolVersion.PROVEN_HEADER_VERSION
            };
            return nodeSettings;
        }

        public static string[] MergeArgs(string[] args)
        {
            var arguments = new List<string>();
            if (args != null)
                arguments.AddRange(args);

            bool isDataDirRootProvided = false;
            bool isIpRangeFilteringProvided = false;
            foreach (var a in arguments)
            {
                if (a.ToLowerInvariant().Contains("datadirroot"))
                    isDataDirRootProvided = true;
                if (a.ToLowerInvariant().Contains("iprangefiltering"))
                    isIpRangeFilteringProvided = true;
            }
            if (!isDataDirRootProvided)
                arguments.Add("datadirroot=Obsidian");
            if (!isIpRangeFilteringProvided)
                arguments.Add("iprangefiltering=0");

            return arguments.ToArray();
        }

        public static void PrintWelcomeMessage(NodeSettings nodeSettings, IFullNode fullNode)
        {

            var welcome = new StringBuilder();
            welcome.AppendLine($"Welcome to Obsidian! Loading network {nodeSettings.Network.Name}...");
            welcome.AppendLine();
            welcome.AppendLine(Properties.Resources.Brand);
            welcome.AppendLine($"{GetCredits()}");
            ((FullNode)fullNode).NodeService<ILoggerFactory>().CreateLogger(GetName())
                .LogInformation(welcome.ToString());
        }

        public static string GetCredits()
        {
            var sb = new StringBuilder();
            var intro = "Credits, greetings and thanks to: ";
            sb.Append(intro);

            var arr = Properties.Resources.Credits.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).OrderBy(x => x).ToList();
            var length = intro.Length;

            foreach (var name in arr)
            {
                if (length + name.Length <= 100)
                {
                    var item = $"{name}, ";
                    length += item.Length;
                    sb.Append(item);
                }
                else
                {
                    sb.Append(Environment.NewLine);
                    length = 0;
                    var item = $"{name}, ";
                    length += item.Length;
                    sb.Append(item);
                }
            }

            var credits = sb.ToString(0, sb.Length - 2);

            return $"{credits} and many more - join the power!";
        }

        public static string GetName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetName().Name;
            var version = assembly.GetShortVersionString();
            // ReSharper disable once UnreachableCode
            var compilation = IsDebug ? " (Debug)" : "";
            return $"{name} {version}{compilation}";
        }

        public static CancellationTokenSource RunIfDebugModeDelayed(IFullNode fullNode, int milliSeconds = 10000)
        {
            var cts = new CancellationTokenSource();

            // #if DEBUG

            var _ = Task.Run(async () =>
            {
                await Task.Delay(milliSeconds, cts.Token);
                TestBench.RunTestCodeAsync((FullNode)fullNode);
            }, cts.Token);

            //  #endif
            return cts;
        }

#if DEBUG
        const bool IsDebug = true;
#else
		public const bool IsDebug = false;
#endif
    }
}
