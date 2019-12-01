using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Consensus.Rules.CommonRules;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Mining;
using Stratis.Bitcoin.Signals;
using Stratis.Bitcoin.Utilities;

namespace Obsidian.Features.X1Wallet
{
    public sealed class NodeServices
    {
        public readonly IBlockProvider BlockProvider;
        public readonly IBlockStore BlockStore;
        public readonly IBroadcasterManager BroadcasterManager;
        public readonly ChainIndexer ChainIndexer;
        public readonly ICoinView CoinView;
        public readonly IConsensusManager ConsensusManager;
        public readonly PosCoinviewRule PosCoinviewRule;
        public readonly DataFolder DataFolder;
        public readonly IDateTimeProvider DateTimeProvider;
        public readonly IInitialBlockDownloadState InitialBlockDownloadState;
        public readonly ILoggerFactory LoggerFactory;
        public readonly INodeLifetime NodeLifetime;
        public readonly ISignals Signals;
        public readonly IStakeChain StakeChain;
        public readonly ITimeSyncBehaviorState TimeSyncBehaviorState;
        public readonly IChainState ChainState;


        public NodeServices(DataFolder dataFolder, ChainIndexer chainIndexer, Network network,
            IBroadcasterManager broadcasterManager, ILoggerFactory loggerFactory,
            INodeLifetime nodeLifetime, ISignals signals, IBlockStore blockStore,
            ITimeSyncBehaviorState timeSyncBehaviorState, IInitialBlockDownloadState initialBlockDownloadState,
            IBlockProvider blockProvider, IConsensusManager consensusManager, IStakeChain stakeChain,
            ICoinView coinView, IDateTimeProvider dateTimeProvider, IChainState chainState)
        {
            C.Network = network;
            Log.SetLogger(loggerFactory.CreateLogger(nameof(X1Wallet)));
            this.LoggerFactory = loggerFactory;
            this.DataFolder = dataFolder;
            this.ChainIndexer = chainIndexer;
            this.BroadcasterManager = broadcasterManager;
            this.NodeLifetime = nodeLifetime;
            this.InitialBlockDownloadState = initialBlockDownloadState;
            this.Signals = signals;
            this.BlockStore = blockStore;
            this.TimeSyncBehaviorState = timeSyncBehaviorState;
            this.BlockProvider = blockProvider;
            this.ConsensusManager = consensusManager;
            this.PosCoinviewRule = consensusManager?.ConsensusRules.GetRule<PosCoinviewRule>();
            this.StakeChain = stakeChain;
            this.DateTimeProvider = dateTimeProvider;
            this.CoinView = coinView;
            this.ChainState = chainState;
           
        }
    }
}
