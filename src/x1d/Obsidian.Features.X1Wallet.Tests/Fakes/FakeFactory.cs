﻿using System;
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

namespace Obsidian.Features.X1Wallet.Tests.Fakes
{
    class FakeFactory
    {
        public static Lazy<FakeFactory> Instance = new Lazy<FakeFactory>(() =>
        {
            var ff = new FakeFactory(null);
            return ff;
        }, true);

        readonly ITestOutputHelper testOutputHelper;

        readonly WalletManagerFactory walletManagerFactory;
        readonly Network network;
        readonly IConnectionManager connectionManager;
        readonly ChainIndexer chainIndexer;
        readonly IBroadcasterManager broadcasterManager;
        readonly IDateTimeProvider dateTimeProvider;

        readonly IFullNode fullNode;
        readonly NodeSettings nodeSettings;
        readonly IChainState chainState;
        readonly INetworkDifficulty networkDifficulty;
        readonly ILoggerFactory loggerFactory;
        readonly ILogger logger;

        public FakeFactory(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;

            this.network = ObsidianXNetworksSelector.Obsidian.Mainnet();
            this.loggerFactory = new TestLoggerFactory(testOutputHelper);
        }

        public WalletController CreateWalletController()
        {

            var controller = new WalletController(
                walletManagerFactory: CreateWalletManagerFactory(),
                connectionManager: null,
                
                chainIndexer: null,
                broadcasterManager: null,
                dateTimeProvider: null,
                fullNode: null, nodeSettings: null,
                chainState: null,
                networkDifficulty: null,
                network: this.network,
                loggerFactory: this.loggerFactory
                );
            return controller;
        }

        public WalletManagerFactory CreateWalletManagerFactory()
        {
            var factory = new WalletManagerFactory(dataFolder: null, chainIndexer: null, network: this.network,
                broadcasterManager: null, this.loggerFactory, nodeLifetime: null, signals: null, blockStore: null,
                timeSyncBehaviorState: null, initialBlockDownloadState: null,
                blockProvider: null, consensusManager: null, stakeChain: null, coinView: null, dateTimeProvider: null);
            return factory;
        }
    }
}