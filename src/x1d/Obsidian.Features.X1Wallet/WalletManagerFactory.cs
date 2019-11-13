using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Mining;
using Stratis.Bitcoin.Signals;
using Stratis.Bitcoin.Utilities;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet
{
    public class WalletManagerFactory : IDisposable
    {
        readonly object lockObject = new object();
        readonly DataFolder dataFolder;
        readonly ChainIndexer chainIndexer;
        readonly Network network;
        readonly IBroadcasterManager broadcasterManager;
        readonly ILoggerFactory loggerFactory;
        readonly INodeLifetime nodeLifetime;
        readonly IInitialBlockDownloadState initialBlockDownloadState;
        readonly ISignals signals;
        readonly IBlockStore blockStore;
        readonly ITimeSyncBehaviorState timeSyncBehaviorState;
        readonly IBlockProvider blockProvider;
        readonly IConsensusManager consensusManager;
        readonly IStakeChain stakeChain;
        readonly ICoinView coinView;
        readonly IDateTimeProvider dateTimeProvider;


        WalletManager walletManager;

        public WalletManagerFactory(DataFolder dataFolder, ChainIndexer chainIndexer, Network network, IBroadcasterManager broadcasterManager, ILoggerFactory loggerFactory,
            INodeLifetime nodeLifetime, ISignals signals, IBlockStore blockStore, ITimeSyncBehaviorState timeSyncBehaviorState, IInitialBlockDownloadState initialBlockDownloadState, IBlockProvider blockProvider, IConsensusManager consensusManager, IStakeChain stakeChain, ICoinView coinView, IDateTimeProvider dateTimeProvider)
        {
            this.dataFolder = dataFolder;
            this.chainIndexer = chainIndexer;
            this.network = network;
            this.broadcasterManager = broadcasterManager;
            this.loggerFactory = loggerFactory;
            this.nodeLifetime = nodeLifetime;
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.signals = signals;
            this.blockStore = blockStore;
            this.timeSyncBehaviorState = timeSyncBehaviorState;
            this.blockProvider = blockProvider;
            this.consensusManager = consensusManager;
            this.stakeChain = stakeChain;
            this.dateTimeProvider = dateTimeProvider;
            this.coinView = coinView;
        }

        public WalletContext GetWalletContext(string walletName, bool doNotCheck = false)
        {
            if (doNotCheck)
            {
                if (this.walletManager == null)
                    return null;
                return new WalletContext(this.walletManager);
            }

            if (walletName == null)
                throw new ArgumentNullException(nameof(walletName));

            if (this.walletManager != null)
            {
                if (this.walletManager.WalletName == walletName)
                    return new WalletContext(this.walletManager);
                throw new InvalidOperationException($"Invalid request for wallet {walletName} - the current wallet is {this.walletManager.WalletName}");
            }
            lock (this.lockObject)
            {
                if (this.walletManager == null)
                {
                    LoadWalletAndCreateWalletManagerInstance(walletName);
                    Debug.Assert(this.walletManager != null, "The WalletSyncManager cannot be correctly initialized when the WalletManager is null");
                }
            }
            return new WalletContext(this.walletManager);

        }

        WalletContext GetWalletContextPrivate()
        {
            return GetWalletContext(null, true);
        }

        void LoadWalletAndCreateWalletManagerInstance(string walletName)
        {
            string x1WalletFilePath = walletName.GetX1WalletFilepath(this.network, this.dataFolder);

            if (!File.Exists(x1WalletFilePath))
                throw new FileNotFoundException($"No wallet file found at {x1WalletFilePath}");


            if (this.walletManager != null)
            {
                if (this.walletManager.CurrentX1WalletFilePath != x1WalletFilePath)
                    throw new NotSupportedException(
                        "Core wallet manager already created, changing the wallet file while node and wallet are running is not currently supported.");
            }
            this.walletManager = new WalletManager(x1WalletFilePath, this.chainIndexer, this.network, this.dataFolder, this.broadcasterManager, this.loggerFactory,
                 this.nodeLifetime, this.timeSyncBehaviorState, this.signals, this.initialBlockDownloadState, this.blockStore, this.blockProvider, this.consensusManager, this.stakeChain, this.coinView, this.dateTimeProvider);
        }

        public void CreateWallet(WalletCreateRequest walletCreateRequest)
        {
            string walletName = walletCreateRequest.WalletName;
            string filePath = walletName.GetX1WalletFilepath(this.network, this.dataFolder);

            if (File.Exists(filePath))
                throw new InvalidOperationException($"A wallet with the name {walletName} already exists at {filePath}!");

            if (string.IsNullOrWhiteSpace(walletCreateRequest.Passphrase))
                throw new InvalidOperationException("A passphrase is required.");

            AddressHelper.Init(this.network);

            var now = DateTime.UtcNow;

            var x1WalletFile = new X1WalletFile
            {
                Addresses = new Dictionary<string, P2WpkhAddress>(),
                WalletGuid = Guid.NewGuid(),
                WalletName = walletName,
                CoinTicker = this.network.CoinTicker,
                CoinType = this.network.Consensus.CoinType,
                CreatedUtc = now,
                ModifiedUtc = now,
                SyncFromHeight = 0, // TODO
                Comment = "Your notes here!",
                Version = 1,
                PassphraseChallenge = KeyHelper.GenerateRandomKeyMaterial(walletCreateRequest.Passphrase,32).EncryptedPrivateKey
            };

            
            var mnemonicBytes = KeyHelper.GetSomewhatImprovedRandom(32);
            var bip39Passphrase = walletCreateRequest.Bip39Passphrase?.Trim() ?? "";

            var wl = Wordlist.English;
            var mnemonic = new Mnemonic(wl, mnemonicBytes);
            byte[] hdSeed = mnemonic.DeriveSeed(bip39Passphrase);

            x1WalletFile.HdSeed = VCL.EncryptWithPassphrase(walletCreateRequest.Passphrase, hdSeed);
            x1WalletFile.HdSeedHasBip39Passphrase = !string.IsNullOrWhiteSpace(bip39Passphrase);

            x1WalletFile.SaveX1WalletFile(filePath);

            X1WalletMetadataFile x1WalletMetadataFile = x1WalletFile.CreateX1WalletMetadataFile(this.network.GenesisHash);
            var x1WalletMetadataFilename = walletName.GetX1WalletMetaDataFilepath(this.network, this.dataFolder);
            x1WalletMetadataFile.SaveX1WalletMetadataFile(x1WalletMetadataFilename);
        }


        public void Dispose()
        {
            using var context = GetWalletContextPrivate();
            context?.WalletManager?.Dispose();

        }


    }
}
