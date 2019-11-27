

using Obsidian.Features.X1Wallet.NodeUI.Logging;

namespace Obsidian.Features.X1Wallet.NodeUI
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Microsoft.Extensions.Logging;
    using NBitcoin;
    using NBitcoin.Protocol;
    using Stratis.Bitcoin.Configuration;

    internal sealed class UiNodeSettings : NodeSettings
    {
        public UiNodeSettings(
            Network network = null,
            ProtocolVersion protocolVersion = SupportedProtocolVersion,
            string agent = "StratisNode",
            string[] args = null,
            NetworksSelector networksSelector = null)
            : base(
                network, protocolVersion, agent, args, networksSelector)
        {
        }

        public void InitLoggerFactory()
        {
            this.LoggerFactory = new MyLoggerFactory();
        }
    }
}
