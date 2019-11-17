using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using Obsidian.Features.X1Wallet.Addresses;
using Obsidian.Features.X1Wallet.Balances;
using Obsidian.Features.X1Wallet.Blockchain;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Api.Responses;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.EventBus;
using Stratis.Bitcoin.EventBus.CoreEvents;
using Stratis.Bitcoin.Features.ColdStaking;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Wallet.Broadcasting;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Mining;
using Stratis.Bitcoin.Signals;
using Stratis.Bitcoin.Utilities;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet
{
    sealed class WalletManager : IDisposable
    {
        #region fields, c'tor and initialisation

        public readonly SemaphoreSlim WalletSemaphore = new SemaphoreSlim(1, 1);

        readonly Network network;
        readonly int coinType;
        readonly ILoggerFactory loggerFactory;
        readonly ILogger logger;
        readonly IBroadcasterManager broadcasterManager;
        readonly ChainIndexer chainIndexer;
        readonly INodeLifetime nodeLifetime;
        readonly ISignals signals;
        readonly IInitialBlockDownloadState initialBlockDownloadState;

        internal IReadOnlyList<PubKeyHashAddress> GetPubKeyHashAddresses(int change, int? take)
        {
            return AddressService.GetPubKeyHashAddresses(change, take, this.X1WalletFile);
        }

        readonly IBlockStore blockStore;

        // for staking
        readonly ITimeSyncBehaviorState timeSyncBehaviorState;
        readonly IBlockProvider blockProvider;
        readonly IConsensusManager consensusManager;
        readonly IStakeChain stakeChain;
        readonly ICoinView coinView;
        readonly IDateTimeProvider dateTimeProvider;

        X1WalletFile X1WalletFile { get; }
        X1WalletMetadataFile Metadata { get; }

        SubscriptionToken blockConnectedSubscription;
        SubscriptionToken transactionReceivedSubscription;

        bool isStartingUp;
        Stopwatch startupStopwatch;
        long startupDuration;
        Timer startupTimer;
        StakingService stakingService;

      

        public WalletManager(string x1WalletFilePath, ChainIndexer chainIndexer, Network network, DataFolder dataFolder,
            IBroadcasterManager broadcasterManager, ILoggerFactory loggerFactory,
            INodeLifetime nodeLifetime, ITimeSyncBehaviorState timeSyncBehaviorState,
            ISignals signals, IInitialBlockDownloadState initialBlockDownloadState, IBlockStore blockStore, IBlockProvider blockProvider, IConsensusManager consensusManager, IStakeChain stakeChain,
            ICoinView coinView, IDateTimeProvider dateTimeProvider
            )
        {
            this.CurrentX1WalletFilePath = x1WalletFilePath;

            this.X1WalletFile = WalletHelper.LoadX1WalletFile(x1WalletFilePath);
            this.CurrentX1WalletMetadataFilePath =
                this.X1WalletFile.WalletName.GetX1WalletMetaDataFilepath(network, dataFolder);
            this.Metadata = WalletHelper.LoadOrCreateX1WalletMetadataFile(this.CurrentX1WalletMetadataFilePath,
                this.X1WalletFile, network.GenesisHash);

            this.chainIndexer = chainIndexer;
            this.network = network;
            this.coinType = network.Consensus.CoinType;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger(typeof(WalletManager).FullName);
            this.nodeLifetime = nodeLifetime;

            this.timeSyncBehaviorState = timeSyncBehaviorState;
            this.blockProvider = blockProvider;
            this.consensusManager = consensusManager;

            this.broadcasterManager = broadcasterManager;
            this.signals = signals;
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.blockStore = blockStore;
            this.stakeChain = stakeChain;

            this.coinView = coinView;
            this.dateTimeProvider = dateTimeProvider;

            ScheduleSyncing();
        }

        public void Dispose()
        {
            StopStaking();

            if (this.broadcasterManager != null)
                this.broadcasterManager.TransactionStateChanged -= OnTransactionStateChanged;

            if (this.transactionReceivedSubscription != null)
                this.signals.Unsubscribe(this.transactionReceivedSubscription);

            if (this.blockConnectedSubscription != null)
                this.signals.Unsubscribe(this.blockConnectedSubscription);
        }

        internal ImportKeysResponse ImportKeys(ImportKeysRequest importKeysRequest)
        {
            // TODO
            return ImportExportService.ImportKeys(importKeysRequest, this.X1WalletFile.PassphraseChallenge);
        }

        internal ExportKeysResponse ExportKeys(ExportKeysRequest exportKeysRequest)
        {
            // TODO
            return ImportExportService.ExportKeys(exportKeysRequest, this.X1WalletFile.PubKeyHashAddresses.Values);
        }

        #endregion

        #region public get-only properties

        public string CurrentX1WalletFilePath { get; }
        public string CurrentX1WalletMetadataFilePath { get; }

        public string WalletName => this.X1WalletFile.WalletName;

        public int WalletLastBlockSyncedHeight => this.Metadata.SyncedHeight;

        public uint256 WalletLastBlockSyncedHash => this.Metadata.SyncedHash;

        #endregion

        #region event handlers

        void OnTransactionStateChanged(object sender, TransactionBroadcastEntry broadcastEntry)
        {
            if (broadcastEntry.State == State.CantBroadcast)
                return;

            try
            {
                this.WalletSemaphore.Wait();

                var memoryPoolEntry = MemoryPoolService.GetMemoryPoolEntry(broadcastEntry.Transaction.GetHash(), this.Metadata.MemoryPool.Entries);

                if (memoryPoolEntry == null)
                {
                    TransactionMetadata walletTransaction = BlockService.AnalyzeTransaction(broadcastEntry.Transaction, this.Metadata.Blocks.Values, GetOwnAddress);
                    if (walletTransaction != null)
                    {
                        var entry = MemoryPoolService.CreateMemoryPoolEntry(walletTransaction, broadcastEntry);
                        this.Metadata.MemoryPool.Entries.Add(entry);
                    }
                }
                else
                {
                    MemoryPoolService.UpdateMemoryPoolEntry(memoryPoolEntry, broadcastEntry);
                }
                SaveMetadata();
            }
            finally
            {
                this.WalletSemaphore.Release();
            }
        }

        internal IEnumerable<PubKeyHashAddress> GetReceiveAddresses(int count, bool v, string passphrase)
        {
            return AddressService.GetReceiveAddresses(count, v, passphrase, this.X1WalletFile);
        }

        internal PubKeyHashAddress GetUnusedChangeAddress(string passphrase, bool isDummy)
        {
            return AddressService.GetUnusedChangeAddress(passphrase, isDummy, this.X1WalletFile);
        }

        void OnTransactionReceived(TransactionReceived transactionReceived)
        {
            try
            {
                this.WalletSemaphore.Wait();

                var walletTransaction = BlockService.AnalyzeTransaction(transactionReceived.ReceivedTransaction, this.Metadata.Blocks.Values, GetOwnAddress);
                if (walletTransaction != null)
                {
                    var entry = MemoryPoolService.CreateMemoryPoolEntry(walletTransaction, null);
                    this.Metadata.MemoryPool.Entries.Add(entry);
                }
                SaveMetadata();
            }
            finally
            {
                this.WalletSemaphore.Release();
            }

        }

        void OnBlockConnected(BlockConnected blockConnected)
        {
            SyncWallet();
        }

        #endregion

      

        #region syncing

        void SyncWallet() // semaphore?
        {
            if (this.initialBlockDownloadState.IsInitialBlockDownload())
            {
                this.logger.LogInformation("Wallet is waiting for IBD to complete.");
                ScheduleSyncing();
                return;
            }

            if (this.chainIndexer.Tip == null || this.chainIndexer.Tip.HashBlock == null)
            {
                this.logger.LogInformation("Waiting for the ChainIndexer to initialize.");
                ScheduleSyncing();
                return;
            }

            this.startupStopwatch?.Restart();

            try
            {
                this.WalletSemaphore.Wait();

                // a) check if the wallet is on the right chain
                if (!IsOnBestChain())
                {
                    MoveToBestChain();
                }

                // if we are here, we are on the best chain and the information about the tip in the Metadata file is correct.

                // b) now let the wallet catch up


                this.logger.LogInformation(
                    $"Wallet {this.WalletName} is at block {this.Metadata.SyncedHeight}, catching up, {TimeSpan.FromMilliseconds(this.startupDuration).Duration()} elapsed.");

                while (this.chainIndexer.Tip.Height > this.Metadata.SyncedHeight)
                {
                    // this can take a long time, so watch for cancellation
                    if (this.nodeLifetime.ApplicationStopping.IsCancellationRequested)
                    {
                        SaveMetadata();
                        return;
                    }

                    if (this.isStartingUp && this.startupStopwatch.ElapsedMilliseconds >= 5000)
                    {
                        SaveMetadata();
                        this.startupDuration += this.startupStopwatch.ElapsedMilliseconds;
                        ScheduleSyncing();
                        return;
                    }

                    var nextBlockForWalletHeight = this.Metadata.SyncedHeight + 1;
                    ChainedHeader nextBlockForWalletHeader = this.chainIndexer.GetHeader(nextBlockForWalletHeight);
                    Block nextBlockForWallet = this.blockStore.GetBlock(nextBlockForWalletHeader.HashBlock);
                    ProcessBlock(nextBlockForWallet, nextBlockForWalletHeader.Height, nextBlockForWallet.GetHash());
                }
            }
            catch (Exception e)
            {
                this.logger.LogError($"{nameof(SyncWallet)}: {e.Message}");
            }
            finally
            {
                this.WalletSemaphore.Release();
            }

            if (this.isStartingUp)
                CompleteStart();
        }

        void ScheduleSyncing()
        {
            this.isStartingUp = true;

            if (this.startupStopwatch == null)
                this.startupStopwatch = new Stopwatch();

            this.startupTimer = new Timer(_ =>
            {
                this.startupTimer.Dispose();
                SyncWallet();

            }, null, 500, Timeout.Infinite);
        }

        void CompleteStart()
        {
            this.isStartingUp = false;
            this.startupStopwatch.Stop();
            this.startupStopwatch = null;
            this.startupTimer = null;
            SaveMetadata();

            if (this.broadcasterManager != null)
                this.broadcasterManager.TransactionStateChanged += OnTransactionStateChanged;
            this.blockConnectedSubscription = this.signals.Subscribe<BlockConnected>(OnBlockConnected);
            this.transactionReceivedSubscription = this.signals.Subscribe<TransactionReceived>(OnTransactionReceived);
        }

      

        /// <summary>
        /// Checks if the wallet is on the right chain.
        /// </summary>
        /// <returns>true, if on the right chain.</returns>
        bool IsOnBestChain()
        {
            bool isOnBestChain;
            if (this.Metadata.SyncedHeight == 0 || this.Metadata.SyncedHash.IsDefaultBlockHash())
            {
                // if the height is 0, we cannot be on the wrong chain
                ResetMetadata();
                isOnBestChain = true;

            }
            else
            {
                // check if the wallet tip hash is in the current consensus chain
                isOnBestChain = this.chainIndexer.GetHeader(this.Metadata.SyncedHash) != null;
            }

            return isOnBestChain;
        }

        /// <summary>
        /// If IsOnBestChain returns false, we need to fix this by removing the fork blocks from the wallet.
        /// </summary>
        void MoveToBestChain()
        {
            ChainedHeader checkpointHeader = null;
            if (!this.Metadata.CheckpointHash.IsDefaultBlockHash())
            {
                var header = this.chainIndexer.GetHeader(this.Metadata.CheckpointHash);
                if (header != null && this.Metadata.CheckpointHeight == header.Height)
                    checkpointHeader = header;  // the checkpoint header is in the correct chain and the the checkpoint height in the wallet is consistent
            }
            if (checkpointHeader != null && this.chainIndexer.Tip.Height - checkpointHeader.Height > this.network.Consensus.MaxReorgLength)  // also check the checkpoint is not newer than it should be
            {
                // we have a valid checkpoint, remove all later blocks
                RemoveBlocks(checkpointHeader);
            }
            else
            {
                // we do not have a usable checkpoint, sync from start by resetting everything
                ResetMetadata();
            }
        }

        /// <summary>
        /// Saves the tip and checkpoint from chainedHeader to the wallet file.
        /// </summary>
        void UpdateLastBlockSyncedAndCheckpoint(int height, uint256 hashBlock)
        {
            this.Metadata.SyncedHeight = height;
            this.Metadata.SyncedHash = hashBlock;

            const int minCheckpointHeight = 500;
            if (height > minCheckpointHeight)
            {
                var checkPoint = this.chainIndexer.GetHeader(height - minCheckpointHeight);
                this.Metadata.CheckpointHash = checkPoint.HashBlock;
                this.Metadata.CheckpointHeight = checkPoint.Height;
            }
            else
            {
                this.Metadata.CheckpointHash = this.network.GenesisHash;
                this.Metadata.CheckpointHeight = 0;
            }
        }

        void ProcessBlock(Block block, int height, uint256 hashBlock)
        {
            var walletBlock = BlockService.AnalyzeBlock(block, this.Metadata.Blocks.Values, GetOwnAddress);

            if (walletBlock != null)
            {
                this.Metadata.Blocks.Add(height, walletBlock);
                MigrateMemoryPoolTransactions(walletBlock.Transactions);

                Log.BlockAddedToWallet(height, walletBlock);
            }

            UpdateLastBlockSyncedAndCheckpoint(height, hashBlock);

            if (!this.isStartingUp)
                SaveMetadata();
        }


        void MigrateMemoryPoolTransactions(IReadOnlyCollection<TransactionMetadata> transactions)
        {
            foreach (var transActionToMigrate in transactions)
            {
                this.Metadata.MemoryPool.Entries.Remove(new MemoryPoolEntry { Transaction = transActionToMigrate });
            }
        }

        /// <summary>
        /// It is assumed that the argument contains the header of the highest block (inclusive) where the wallet data is
        /// consistent with the right chain.
        /// This method removes all block and the transactions in them of later blocks.
        /// </summary>
        /// <param name="checkpointHeader">ChainedHeader of the checkpoint</param>
        public void RemoveBlocks(ChainedHeader checkpointHeader)
        {
            var blocksAfterCheckpoint = this.Metadata.Blocks.Keys.Where(x => x > checkpointHeader.Height).ToArray();
            foreach (var height in blocksAfterCheckpoint)
                this.Metadata.Blocks.Remove(height);


            // Update last block synced height
            this.Metadata.SyncedHeight = checkpointHeader.Height;
            this.Metadata.SyncedHash = checkpointHeader.HashBlock;
            this.Metadata.CheckpointHeight = checkpointHeader.Height;
            this.Metadata.CheckpointHash = checkpointHeader.HashBlock;
            SaveMetadata();
        }

        /// <summary>
        /// Clears and initializes the wallet Metadata file, and sets heights to 0 and the hashes to null,
        /// and saves the Metadata file, effectively updating it to the latest version.
        /// </summary>
        internal void ResetMetadata()
        {
            this.Metadata.SyncedHash = this.network.GenesisHash;
            this.Metadata.SyncedHeight = 0;
            this.Metadata.CheckpointHash = this.Metadata.SyncedHash;
            this.Metadata.CheckpointHeight = 0;
            this.Metadata.Blocks = new Dictionary<int, BlockMetadata>();
            this.Metadata.WalletGuid = this.X1WalletFile.WalletGuid;

            SaveMetadata();
        }

        #endregion


        internal LoadWalletResponse LoadWallet()
        {
            return new LoadWalletResponse { PassphraseChallenge = this.X1WalletFile.PassphraseChallenge.ToHexString() };
        }

        internal string EnsureDummyMultiSig1Of2Address()
        {
            var passphrase = "passwordpassword";

            if (this.X1WalletFile.HdSeed == null)
            {
                var hdBytes = new byte[32];
                var Rng = new RNGCryptoServiceProvider();
                Rng.GetBytes(hdBytes);
                var wl = Wordlist.English;
                var mnemonic = new Mnemonic(wl, hdBytes);
                byte[] hdSeed = mnemonic.DeriveSeed("");
                this.X1WalletFile.HdSeed = VCL.EncryptWithPassphrase(passphrase, hdSeed);
            }


            var seed = VCL.DecryptWithPassphrase(passphrase, this.X1WalletFile.HdSeed);

            // own key
            KeyMaterial myKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, this.coinType, AddressType.MultiSig, 0, 0);
            PubKey myPubKey = myKeyMaterial.GetKey(passphrase).PubKey.Compress();

            // other Key
            KeyMaterial otherKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, this.coinType, AddressType.MultiSig, 0, 1);
            var otherPubKey = otherKeyMaterial.GetKey(passphrase).PubKey.Compress();

            // The redeem script looks like:
            // 1 03fad6426522dbda5c5a9f8cab24a54ccc374517ad8790bf7e5a14308afc1bf77b 0340ecf2e20978075a49369e35269ecf0651d2f48061ebbf918f3eb1964051f65c 2 OP_CHECKMULTISIG
            Script redeemScript = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(1, myPubKey, otherPubKey);

            // The address looks like:
            // odx1qvar8r29r8llzj53q5utmcewpju59263h38250ws33lp2q45lmalqg5lmdd
            string bech32ScriptAddress = redeemScript.WitHash.GetAddress(this.network).ToString();

            // In P2SH payments, we refer to the hash of the Redeem Script as the scriptPubKey. It looks like:
            // 0 674671a8a33ffe295220a717bc65c19728556a3789d547ba118fc2a0569fdf7e
            Script scriptPubKey = redeemScript.WitHash.ScriptPubKey;
            var scp = scriptPubKey.ToString();

            throw new NotImplementedException();
            //if (this.X1WalletFile.ScriptAddresses == null)
            //    this.X1WalletFile.ScriptAddresses = new Dictionary<string, P2WshAddress>();

            //if (this.X1WalletFile.ScriptAddresses.ContainsKey(bech32ScriptAddress))
            //    this.X1WalletFile.ScriptAddresses.Remove(bech32ScriptAddress);

            //if (!this.X1WalletFile.ScriptAddresses.ContainsKey(bech32ScriptAddress))
            //{
            //    this.X1WalletFile.ScriptAddresses.Add(bech32ScriptAddress,
            //        new P2WshAddress
            //        {
            //            Address = bech32ScriptAddress,
            //            ScriptPubKey = scriptPubKey.ToBytes(),
            //            RedeemScript = redeemScript.ToBytes(),
            //            AddressType = AddressType.MultiSig,
            //            CompressedPublicKey = myPubKey.ToBytes(),
            //            EncryptedPrivateKey = myKeyMaterial.EncryptedPrivateKey,
            //            Description = "My and Bob's 1-of-2 MultiSig account",
            //            PartnerPublicKeys = new[]{ new PartnerPublicKey
            //            {
            //                Label = "Bob",
            //                CompressedPublicKey=otherPubKey.ToBytes()
            //            }}
            //        });
            //    this.X1WalletFile.SaveX1WalletFile(this.CurrentX1WalletFilePath);
            //}
            //return bech32ScriptAddress;
        }

        internal ColdStakingAddress EnsureColdStakingAddress(string passphrase)
        {

            if (this.X1WalletFile.ColdStakingAddresses != null && this.X1WalletFile.ColdStakingAddresses.Count > 0)
            {
                return this.X1WalletFile.ColdStakingAddresses.Values.First();
            }

            this.X1WalletFile.ColdStakingAddresses = new Dictionary<string, ColdStakingAddress>();

            var seed = VCL.DecryptWithPassphrase(passphrase, this.X1WalletFile.HdSeed);

            KeyMaterial coldKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, this.coinType, AddressType.ColdStakingCold, 0, 0);
            PubKey coldPubKey = coldKeyMaterial.GetKey(passphrase).PubKey.Compress();

            KeyMaterial hotKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, this.coinType, AddressType.ColdStakingHot, 0, 0);
            PubKey hotPubKey = hotKeyMaterial.GetKey(passphrase).PubKey.Compress();

            Script csRedeemScript = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKey.WitHash.AsKeyId(), coldPubKey.WitHash.AsKeyId());

            // In P2SH payments, we refer to the hash of the Redeem Script as the scriptPubKey. It looks like:
            // 0 674671a8a33ffe295220a717bc65c19728556a3789d547ba118fc2a0569fdf7e
            Script scriptPubKey = csRedeemScript.WitHash.ScriptPubKey;

            string bech32ScriptAddress = csRedeemScript.WitHash.GetAddress(this.network).ToString();



            var scp = scriptPubKey.ToString();
            throw new NotImplementedException();
            //if (this.X1WalletFile.ScriptAddresses == null)
            //    this.X1WalletFile.ScriptAddresses = new Dictionary<string, P2WshAddress>();

            //if (this.X1WalletFile.ScriptAddresses.ContainsKey(bech32ScriptAddress))
            //    this.X1WalletFile.ScriptAddresses.Remove(bech32ScriptAddress);

            //if (!this.X1WalletFile.ScriptAddresses.ContainsKey(bech32ScriptAddress))
            //{
            //    //this.X1WalletFile.ScriptAddresses.Add(bech32ScriptAddress,
            //    //    new P2WshAddress
            //    //    {
            //    //        Address = bech32ScriptAddress,
            //    //        ScriptPubKey = scriptPubKey.ToBytes(),
            //    //        RedeemScript = redeemScript.ToBytes(),
            //    //        AddressType = AddressType.HdMultiSig,
            //    //        CompressedPublicKey = myMultiSigPublicKeyBytes,
            //    //        EncryptedPrivateKey = myMultiSigPrivate.EncryptedPrivateKey,
            //    //        Description = "My and Bob's 1-of-2 MultiSig account",
            //    //        PartnerPublicKeys = new[]{ new PartnerPublicKey
            //    //        {
            //    //            Label = "Bob",
            //    //            CompressedPublicKey=partnerMultiSigPublicKeyBytes2
            //    //        }}
            //    //    });
            //    //this.X1WalletFile.SaveX1WalletFile(this.CurrentX1WalletFilePath);
            //}

            //var scpAsString = scriptPubKey.ToString();
            //var paymentScript = scriptPubKey.PaymentScript;
            //var paymentScriptAsString = paymentScript.ToString();

            //var pswsh = scriptPubKey.WitHash.ScriptPubKey;
            //var p2wshadr = scriptPubKey.WitHash.GetAddress(this.network);

            return new ColdStakingAddress();
        }




        #region public methods


       

        public Balance GetBalance(string matchAddress = null, AddressType matchAddressType = AddressType.MatchAll)
        {
            return BalanceService.GetBalance(this.Metadata.Blocks, this.Metadata.SyncedHeight,
                this.Metadata.MemoryPool.Entries, GetOwnAddress, matchAddress, matchAddressType);
        }

        ISegWitAddress GetOwnAddress(string bech32Address)
        {
            return this.X1WalletFile.FindAddress(bech32Address);
        }

        internal WalletDetails GetWalletDetails()
        {
            var info = new WalletDetails
            {
                WalletName = this.WalletName,
                WalletFilePath = this.CurrentX1WalletFilePath,
                SyncedHeight = this.Metadata.SyncedHeight,
                SyncedHash = this.Metadata.SyncedHash,
                Adresses = this.X1WalletFile.PubKeyHashAddresses.Count,
                MultiSigAddresses = this.X1WalletFile.MultiSigAddresses.Count,
                ColdStakingAddresses = this.X1WalletFile.ColdStakingAddresses.Count,
                StakingInfo = GetStakingInfo(),
                MemoryPool = this.Metadata.MemoryPool,
                PassphraseChallenge = this.X1WalletFile.PassphraseChallenge.ToHexString()

            };

            try
            {
                var unusedReceiveAddress = AddressService.GetUnusedReceiveAddress(this.X1WalletFile);
                info.UnusedAddress = unusedReceiveAddress.Address;  // this throws if unusedReceiveAddress is null, which would be a bug
            }
            catch (X1WalletException e)
            {
                if (e.HttpStatusCode != HttpStatusCode.NotFound)
                    throw;
                info.UnusedAddress = e.Message;
            }


            info.Balance = GetBalance();

            if (info.MemoryPool.Entries.Count > 0)
                ;
            return info;
        }

        StakingInfo GetStakingInfo()
        {
            if (this.stakingService == null)
                return new StakingInfo();

            var stakingInfo = new StakingInfo
            {
                Enabled = true,
                PosV3 = this.stakingService.PosV3,
                StakingStatus = this.stakingService.Status,
                LastStakedBlock = this.stakingService.LastStakedBlock,
            };

            if (stakingInfo.LastStakedBlock.Hash == null)
                stakingInfo.LastStakedBlock = null;
            return stakingInfo;
        }

        public HistoryInfo GetHistoryInfo(HistoryRequest historyRequest)
        {
            var transactionCount = 0;
            var historyInfo = new HistoryInfo { Blocks = new List<HistoryInfo.BlockInfo>() };

            var unconfirmed = new List<HistoryInfo.TransactionInfo>();
            foreach (var item in this.Metadata.MemoryPool.Entries.OrderByDescending(x => x.TransactionTime))
            {
                var tx = item.Transaction;
                var ti = new HistoryInfo.TransactionInfo { TxType = tx.TxType, HashTx = tx.HashTx.ToString(), ValueAdded = tx.ValueAdded };
                unconfirmed.Add(ti);
                if (tx.Received != null)
                    ti.TotalReceived = tx.Received.Values.Sum(x => x.Satoshis);
                if (tx.Spent != null)
                    ti.TotalSpent = tx.Spent.Values.Sum(x => x.Satoshis);
                if (tx.Destinations != null)
                {
                    ti.Recipients = new Recipient[tx.Destinations.Count];
                    int recipientIndex = 0;
                    foreach (var d in tx.Destinations)
                    {
                        var r = new Recipient { Address = d.Value.Address, Amount = d.Value.Satoshis };
                        ti.Recipients[recipientIndex++] = r;
                    }
                }
                transactionCount++;
            }
            if (unconfirmed.Count > 0)
            {
                var bi = new HistoryInfo.BlockInfo { Height = int.MaxValue, Time = this.Metadata.MemoryPool.Entries.Max(x => x.TransactionTime), Transactions = unconfirmed };
                historyInfo.Blocks.Add(bi);
            }


            foreach ((int height, BlockMetadata block) in this.Metadata.Blocks.Reverse())
            {
                var transactions = new List<HistoryInfo.TransactionInfo>();

                foreach (var tx in block.Transactions.Reverse())
                {
                    if (historyRequest.Take.HasValue)
                        if (transactionCount == historyRequest.Take.Value)
                            return historyInfo;

                    var ti = new HistoryInfo.TransactionInfo { TxType = tx.TxType, HashTx = tx.HashTx.ToString(), ValueAdded = tx.ValueAdded };
                    transactions.Add(ti);

                    if (tx.Received != null)
                        ti.TotalReceived = tx.Received.Values.Sum(x => x.Satoshis);
                    if (tx.Spent != null)
                        ti.TotalSpent = tx.Spent.Values.Sum(x => x.Satoshis);
                    if (tx.Destinations != null)
                    {
                        ti.Recipients = new Recipient[tx.Destinations.Count];
                        int recipientIndex = 0;
                        foreach (var d in tx.Destinations)
                        {
                            var r = new Recipient { Address = d.Value.Address, Amount = d.Value.Satoshis };
                            ti.Recipients[recipientIndex++] = r;
                        }
                    }
                    transactionCount++;
                }

                if (transactions.Count > 0)
                {
                    var bi = new HistoryInfo.BlockInfo { Height = height, Time = block.Time, HashBlock = block.HashBlock.ToString(), Transactions = transactions };
                    historyInfo.Blocks.Add(bi);
                }
            }
            return historyInfo;
        }


      


        public void StartStaking(string passphrase)
        {
            Guard.NotNull(passphrase, nameof(passphrase));

            if (VCL.DecryptWithPassphrase(passphrase, this.X1WalletFile.PassphraseChallenge) == null)
                throw new X1WalletException(HttpStatusCode.Unauthorized, "The passphrase is not correct.");

            if (!this.network.Consensus.IsProofOfStake)
                throw new X1WalletException(HttpStatusCode.BadRequest, "Staking requires a Proof-of-Stake consensus.");

            if (this.timeSyncBehaviorState.IsSystemTimeOutOfSync)
            {
                string errorMessage = "Staking cannot start, your system time does not match that of other nodes on the network." + Environment.NewLine
                                                                                                                                  + "Please adjust your system time and restart the node.";
                this.logger.LogError(errorMessage);
                throw new X1WalletException(HttpStatusCode.InternalServerError, errorMessage);
            }

            if (this.stakingService == null)
            {
                this.stakingService = new StakingService(this, passphrase, this.loggerFactory, this.network, this.blockProvider, this.consensusManager, this.stakeChain, this.coinView, this.dateTimeProvider);
                this.stakingService.Start();
            }
        }

        internal void StopStaking()
        {
            if (this.stakingService != null)
            {
                this.stakingService.Stop();
                this.stakingService = null;
            }
        }

        #endregion

       





        void SaveMetadata()
        {
            this.Metadata.SaveX1WalletMetadataFile(this.CurrentX1WalletMetadataFilePath);
            this.logger.LogInformation("Wallet saved.");
        }


        public PubKeyHashAddress GetUnusedReceiveAddress()
        {
            return AddressService.GetUnusedReceiveAddress(this.X1WalletFile);
        }
    }
}
