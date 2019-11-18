using System.Collections.Generic;
using System.Threading.Tasks;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.EventBus;
using Stratis.Bitcoin.EventBus.CoreEvents;
using Stratis.Bitcoin.Primitives;
using Stratis.Bitcoin.Signals;

namespace Obsidian.Features.X1Wallet.Tests.Fakes
{
    using System;
    using Microsoft.Extensions.Logging;
    using NBitcoin;
    using Obsidian.Networks.ObsidianX;
    using Stratis.Bitcoin;
    using Stratis.Bitcoin.Base;
    using Stratis.Bitcoin.Configuration;
    using Stratis.Bitcoin.Connection;
    using Stratis.Bitcoin.Features.Wallet.Interfaces;
    using Stratis.Bitcoin.Interfaces;
    using Stratis.Bitcoin.Utilities;
    using Xunit.Abstractions;

    class FakeFactory
    {
        readonly ITestOutputHelper testOutputHelper;

        public WalletManagerFactory WalletManagerFactory;

        public readonly Network Network;

        public readonly NodeServices NodeServices;

        public readonly TestInitialBlockDownloadState InitialBlockDownloadState;
        public readonly ChainIndexer ChainIndexer;
        public readonly ISignals Signals;
        public readonly INodeLifetime NodeLifetime;
        public readonly TestBlockStore BlockStore;

        readonly IConnectionManager connectionManager;
       
        readonly IBroadcasterManager broadcasterManager;
        readonly IDateTimeProvider dateTimeProvider;

        readonly IFullNode fullNode;
        readonly NodeSettings nodeSettings;
        readonly IChainState chainState;
        readonly INetworkDifficulty networkDifficulty;
        readonly ILoggerFactory loggerFactory;
        public readonly DataFolder DataFolder;
        readonly ILogger logger;
      

        public FakeFactory(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;

            this.Network = ObsidianXNetworksSelector.Obsidian.Mainnet();
            this.loggerFactory = new TestLoggerFactory(testOutputHelper);
            this.nodeSettings = Obsidian.x1d.Util.Init.GetNodeSettings(new string[0]);
            this.DataFolder = this.nodeSettings.DataFolder;
            this.InitialBlockDownloadState = new TestInitialBlockDownloadState() {IBD = true};
            this.ChainIndexer = new ChainIndexer(this.Network);
            this.Signals = new Signals(this.loggerFactory, new DefaultSubscriptionErrorHandler(this.loggerFactory));
            this.NodeLifetime = new NodeLifetime();
            this.BlockStore = new TestBlockStore();

            this.NodeServices = new NodeServices(dataFolder: this.DataFolder, chainIndexer: this.ChainIndexer, network: this.Network,
                broadcasterManager: new TestBroadcastManager(), this.loggerFactory, nodeLifetime: this.NodeLifetime, signals: this.Signals, blockStore: this.BlockStore,
                timeSyncBehaviorState: null, initialBlockDownloadState: this.InitialBlockDownloadState,
                blockProvider: null, consensusManager: null, stakeChain: null, coinView: null, dateTimeProvider: null, chainState: null);
            this.WalletManagerFactory = new WalletManagerFactory(this.NodeServices);
        }

        public void DisposeAndRecreateWalletManagerFactory()
        {
            this.WalletManagerFactory.Dispose();
            this.WalletManagerFactory = CreateWalletManagerFactory();
        }

        public WalletController CreateWalletController()
        {

            var controller = new WalletController(
                walletManagerFactory: CreateWalletManagerFactory(),
                connectionManager: null,
                
                chainIndexer: null,
                broadcasterManager: null,
                dateTimeProvider: null,
                fullNode: null,
                nodeSettings: this.nodeSettings,
                chainState: null,
                networkDifficulty: null,
                network: this.Network,
                loggerFactory: this.loggerFactory
                );
            return controller;
        }

        internal void SyncToHeight(int height, string walletName)
        {
            while (true)
            {
                using (var context = this.WalletManagerFactory.AutoLoad(walletName))
                {
                    if (context.WalletManager.WalletLastBlockSyncedHeight == height)
                        break;
                }
                Task.Delay(1000).Wait();
                this.Signals.Publish(new BlockConnected(null));
            }
        }

        internal Script GetScriptPubKeyForMining(string walletName)
        {
            using (var context = this.WalletManagerFactory.AutoLoad(walletName))
            {
                return context.WalletManager.GetPubKeyHashAddresses(C.External, 1)[0].GetScriptPubKey();
            }
        }

        WalletManagerFactory CreateWalletManagerFactory()
        {
            return new WalletManagerFactory(this.NodeServices);
        }

        public void AddBlockToBlockStore(Block block)
        {
            this.BlockStore.Blocks[block.GetHash()] = block;
        }

        public ChainedHeaderBlock AddNextBlock(List<Transaction> transactions)
        {
            ChainedHeader previous = this.ChainIndexer.Tip;

            BlockHeader nextHeader = this.Network.Consensus.ConsensusFactory.CreateBlockHeader();
            nextHeader.HashPrevBlock = previous.HashBlock;

            ChainedHeader chainedHeader = new ChainedHeader(nextHeader, nextHeader.GetHash(), previous);

            var block = new PosBlock(chainedHeader.Header);

            foreach (var tx in transactions)
                block.AddTransaction(tx);

            chainedHeader.Block = block;

            this.ChainIndexer.Add(chainedHeader);
            this.AddBlockToBlockStore(block);

            return new ChainedHeaderBlock(block, chainedHeader);
        }

        internal void SetIBD(bool isInInitialBlockDownLoad)
        {
            this.InitialBlockDownloadState.IBD = isInInitialBlockDownLoad;
        }
    }
}
