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
using Obsidian.Features.X1Wallet.Balances;
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
using static Obsidian.Features.X1Wallet.Tools.Extensions;

namespace Obsidian.Features.X1Wallet
{
    sealed class WalletManager : IDisposable
    {
        public readonly SemaphoreSlim WalletSemaphore = new SemaphoreSlim(1, 1);

        readonly object budgetLock = new object();

        readonly Network network;
        readonly int coinType;
        readonly ILoggerFactory loggerFactory;
        readonly ILogger logger;
        readonly IBroadcasterManager broadcasterManager;
        readonly ChainIndexer chainIndexer;
        readonly INodeLifetime nodeLifetime;
        readonly ISignals signals;
        readonly IInitialBlockDownloadState initialBlockDownloadState;
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

        #region c'tor and initialisation

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
                var unusedReceiveAddress = GetUnusedReceiveAddress();
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

        internal LoadWalletResponse LoadWallet()
        {
            return new LoadWalletResponse { PassphraseChallenge = this.X1WalletFile.PassphraseChallenge.ToHexString() };
        }

        #endregion


        #region public get-only properties

        public string CurrentX1WalletFilePath { get; }
        public string CurrentX1WalletMetadataFilePath { get; }

        public string WalletName => this.X1WalletFile.WalletName;

        public int WalletLastBlockSyncedHeight => this.Metadata.SyncedHeight;

        public uint256 WalletLastBlockSyncedHash => this.Metadata.SyncedHash;

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
                    ProcessBlock(nextBlockForWallet, nextBlockForWalletHeader);
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

        #endregion


        void OnTransactionStateChanged(object sender, TransactionBroadcastEntry broadcastEntry)
        {
            if (broadcastEntry.State == State.CantBroadcast)
                return;

            try
            {
                this.WalletSemaphore.Wait();

                var memoryPoolEntry = GetMemoryPoolEntry(broadcastEntry.Transaction.GetHash());

                if (memoryPoolEntry == null)
                {
                    var processed = ProcessTransaction(broadcastEntry.Transaction);
                    if (processed != null)
                    {
                        var entry = new MemoryPoolEntry
                        {
                            Transaction = processed,
                            BroadcastState = broadcastEntry.State.ToBroadcastState(),
                            MemoryPoolError = broadcastEntry.MempoolError.GetMemoryPoolError(),
                            ConsensusError = broadcastEntry.MempoolError.GetMemoryPoolError(),
                            TransactionTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        this.Metadata.MemoryPool.Entries.Add(entry);
                    }
                }
                else
                {
                    UpdateMemoryPoolEntry(memoryPoolEntry, broadcastEntry);
                }
                SaveMetadata();
            }
            finally
            {
                this.WalletSemaphore.Release();
            }
        }

        void UpdateMemoryPoolEntry(MemoryPoolEntry memoryPoolEntry, TransactionBroadcastEntry broadcastEntry)
        {
            var newState = broadcastEntry.State.ToBroadcastState();
            var newErrorM = broadcastEntry.MempoolError.GetMemoryPoolError();
            var newErrorC = broadcastEntry.MempoolError.GetMemoryPoolConsensusError();

            var sb = new StringBuilder();
            sb.AppendLine();
            if (newState != memoryPoolEntry.BroadcastState)
                sb.AppendLine($"BroadcastState {memoryPoolEntry.BroadcastState} -> {newState}");
            if (newErrorM != memoryPoolEntry.MemoryPoolError)
                sb.AppendLine($"MemoryPoolError {memoryPoolEntry.MemoryPoolError} -> {newErrorM}");
            if (newErrorC != memoryPoolEntry.ConsensusError)
                sb.AppendLine($"MemoryPoolError {memoryPoolEntry.ConsensusError} -> {newErrorC}");

            this.logger.LogInformation(
                $"Updating Tracked tx {memoryPoolEntry.Transaction.HashTx},  changes: {sb}");

            memoryPoolEntry.BroadcastState = newState;
            memoryPoolEntry.MemoryPoolError = newErrorM;
            memoryPoolEntry.ConsensusError = newErrorC;
        }

        MemoryPoolEntry GetMemoryPoolEntry(uint256 hashTx)
        {
            this.Metadata.MemoryPool.Entries.TryGetValue(
                new MemoryPoolEntry { Transaction = new TransactionMetadata { HashTx = hashTx } },
                out var existingEntry);
            return existingEntry;
        }

        void OnTransactionReceived(TransactionReceived transactionReceived)
        {
            try
            {
                this.WalletSemaphore.Wait();

                var processed = ProcessTransaction(transactionReceived.ReceivedTransaction);
                if (processed != null)
                {
                    this.Metadata.MemoryPool.Entries.Add(new MemoryPoolEntry
                    {
                        Transaction = processed,
                        TransactionTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
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
            if (blockConnected?.ConnectedBlock?.ChainedHeader != null)
                this.logger.LogInformation($"Block {blockConnected.ConnectedBlock.ChainedHeader.Height} connected.");
            SyncWallet();
        }

        #region import/export keys

        public ImportKeysResponse ImportKeys(ImportKeysRequest importKeysRequest)
        {
            if (importKeysRequest == null)
                throw new ArgumentNullException(nameof(importKeysRequest));
            if (importKeysRequest.WalletPassphrase == null)
                throw new ArgumentNullException(nameof(importKeysRequest.WalletPassphrase));
            if (importKeysRequest.Keys == null)
                throw new ArgumentNullException(nameof(importKeysRequest.Keys));

            var delimiters = new HashSet<char>();
            foreach (var c in importKeysRequest.Keys.Trim().ToCharArray())
            {
                if (char.IsWhiteSpace(c))
                    delimiters.Add(c);
            }

            var items = importKeysRequest.Keys.Split(delimiters.ToArray());
            var possibleKeys = items.Where(i => i.Length == 52).Distinct().ToList();
            if (possibleKeys.Count == 0)
                throw new X1WalletException(HttpStatusCode.BadRequest, "Input material cointained no keys.");

            var test = VCL.DecryptWithPassphrase(importKeysRequest.WalletPassphrase, this.X1WalletFile.PassphraseChallenge);
            if (test == null)
                throw new X1WalletException(HttpStatusCode.Unauthorized,
                    "Your passphrase is incorrect.");
            var importedAddresses = new List<string>();

            var obsidianNetwork = new ObsidianNetwork();

            foreach (var candidate in possibleKeys)
            {
                try
                {
                    var secret = new BitcoinSecret(candidate, obsidianNetwork);
                    var privateKey = secret.PrivateKey.ToBytes();
                    throw new NotImplementedException();
                    //var address = AddressHelper.CreateWithPrivateKey(privateKey, importKeysRequest.WalletPassphrase, AddressType.SingleKey);

                    //this.X1WalletFile.Addresses.Add(address.Address, address);
                    //importedAddresses.Add($"{secret.GetAddress()} -> {address.Address}");
                }
                catch (Exception e)
                {
                    this.logger.LogWarning($"Could not import '{candidate}' as key or address. {e.Message}");
                }

            }

            this.X1WalletFile.SaveX1WalletFile(this.CurrentX1WalletFilePath);

            var response = new ImportKeysResponse
            { ImportedAddresses = importedAddresses, Message = $"Imported {importedAddresses.Count} addresses." };
            return response;
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

        internal ExportKeysResponse ExportKeys(ExportKeysRequest exportKeysRequest)
        {
            var header = new StringBuilder();
            header.AppendLine($"Starting export from wallet {this.X1WalletFile.WalletName}, network {this.network.Name} on {DateTime.UtcNow} UTC.");
            var errors = new StringBuilder();
            errors.AppendLine("Errors");
            var success = new StringBuilder();
            success.AppendLine("Exported Private Key (Hex); Unix Time UTC; IsChange; Address; Label:");
            int errorCount = 0;
            int successCount = 0;
            try
            {
                // TODO: also support export of coldstaking and multisig addresses
                var addresses = this.X1WalletFile.PubKeyHashAddresses.Values;
                header.AppendLine($"{this.X1WalletFile.PubKeyHashAddresses.Count} found in wallet.");

                var enc = new Bech32Encoder($"{this.network.CoinTicker.ToLowerInvariant()}key");

                foreach (var a in addresses)
                {
                    try
                    {
                        var decryptedKey = VCL.DecryptWithPassphrase(exportKeysRequest.WalletPassphrase, a.KeyMaterial.EncryptedPrivateKey);
                        if (decryptedKey == null)
                        {
                            errorCount++;
                            header.AppendLine(
                                $"Address '{a.Address}'  could not be decrpted with this passphrase.");
                        }
                        else
                        {
                            var privateKey = enc.Encode(0, decryptedKey);
                            success.AppendLine($"{privateKey};{a.Address}");
                            successCount++;
                        }
                    }
                    catch (Exception e)
                    {
                        header.AppendLine($"Exception processing Address '{a.Address}': {e.Message}");
                    }
                }

                header.AppendLine($"{errorCount} errors occured.");
                header.AppendLine($"{successCount} addresses with private keys successfully exported.");
            }
            catch (Exception e)
            {
                errors.AppendLine(e.Message);
                return new ExportKeysResponse { Message = $"Export failed because an exception occured: {e.Message}" };
            }

            return new ExportKeysResponse
            { Message = $"{header}{Environment.NewLine}{success}{Environment.NewLine}{errors}{Environment.NewLine}" };
        }


        #endregion


        #region public methods





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

        /// <summary>
        /// Gets an unused receive address or throws en exception.
        /// </summary>
        public PubKeyHashAddress GetUnusedReceiveAddress()
        {
            return this.X1WalletFile.GetReceiveAddress(false, null);
        }

        public PubKeyHashAddress GetUnusedChangeAddress(string passphrase, bool isDummy)
        {
            return this.X1WalletFile.GetChangeAddress(passphrase, isDummy);
        }



        public PubKeyHashAddress[] GetReceiveAddresses(int count, bool used, string passphrase)
        {
            if (used)
            {
                return this.X1WalletFile.PubKeyHashAddresses.Values.Take(count).ToArray();
            }
            else
            {
                return this.X1WalletFile.CreateNewAddresses(C.External, passphrase, count).ToArray();
            }

        }

        public PubKeyHashAddress[] GetPubKeyHashAddresses(int isChange, int? take)
        {
            return take.HasValue
                ? this.X1WalletFile.PubKeyHashAddresses.Values.Where(x => x.KeyMaterial.IsChange == isChange).Take(take.Value).ToArray()
                : this.X1WalletFile.PubKeyHashAddresses.Values.Where(x => x.KeyMaterial.IsChange == isChange).ToArray();
        }

        public Balance GetBalance(string matchAddress = null, AddressType matchAddressType = AddressType.MatchAll)
        {
            return BalanceService.GetBalance(this.Metadata.Blocks, this.Metadata.SyncedHeight,
                this.Metadata.MemoryPool.Entries, GetOwnAddress, matchAddress, matchAddressType);
        }

        ISegWitAddress GetOwnAddress(string bech32Address)
        {
            return this.X1WalletFile.FindAddress(bech32Address);
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





        void ProcessBlock(Block block, ChainedHeader chainedHeader)
        {
            var blockHeight = chainedHeader.Height;

            foreach (Transaction transaction in block.Transactions)
            {
                if (blockHeight == 275 && !transaction.IsProtocolTransaction())
                    ;
                var walletTransaction = ProcessTransaction(transaction);

                if (walletTransaction != null)
                {
                    this.Metadata.MemoryPool.Entries.Remove(new MemoryPoolEntry { Transaction = walletTransaction });

                    if (!this.Metadata.Blocks.TryGetValue(blockHeight, out BlockMetadata walletBlock))
                    {
                        walletBlock = new BlockMetadata { HashBlock = block.GetHash(), Time = block.Header.Time };
                        this.Metadata.Blocks.Add(blockHeight, walletBlock);
                    }
                    walletBlock.Transactions.Add(walletTransaction);
                    this.logger.LogInformation($"Confirmed transaction {walletTransaction.HashTx} in block {blockHeight} added {walletTransaction.ValueAdded / 100000000m} {this.network.CoinTicker} to the wallet.");
                }
            }

            UpdateLastBlockSyncedAndCheckpoint(chainedHeader);

            if (!this.isStartingUp)
                SaveMetadata();
        }

        TransactionMetadata ProcessTransaction(Transaction transaction)
        {
            var spent = ExtractOutgoingFunds(transaction, out var amountSpent);
            var received = ExtractIncomingFunds(transaction, spent != null, out var amountReceived, out var destinations);

            if (received == null && spent == null)
                return null;

            var walletTransaction = new TransactionMetadata
            {
                TxType = GetTxType(transaction, received, destinations, spent),
                HashTx = transaction.GetHash(),
                Received = received,
                Destinations = destinations,
                Spent = spent,
                ValueAdded = amountReceived - amountSpent
            };

            return walletTransaction;
        }

        Dictionary<string, UtxoMetadata> ExtractIncomingFunds(Transaction transaction, bool didSpend, out long amountReceived, out Dictionary<string, UtxoMetadata> destinations)
        {
            Dictionary<string, UtxoMetadata> received = null;
            Dictionary<string, UtxoMetadata> notInWallet = null;
            long sum = 0;
            int index = 0;

            foreach (var output in transaction.Outputs)
            {
                ISegWitAddress ownAddress = null;

                if (!output.IsProtocolOutput(transaction))
                    ownAddress = GetOwnAddress(output.ScriptPubKey.GetAddressFromScriptPubKey());

                if (ownAddress != null)
                {
                    NotNull(ref received, transaction.Outputs.Count);

                    var item = new UtxoMetadata
                    {
                        Address = ownAddress.Address,
                        HashTx = transaction.GetHash(),
                        Satoshis = output.Value.Satoshi,
                        Index = index
                    };
                    received.Add(item.GetKey(), item);
                    sum += item.Satoshis;
                }
                else
                {   // For protocol tx, we are not interested in the other outputs.
                    // If we spent, the save the destinations, because the wallet wants to show where we sent coins to.
                    // if we did not spent, we do not save the destinations, because they are the other parties change address
                    // and we only received coins.
                    if (!transaction.IsCoinBase && !transaction.IsCoinStake && didSpend)
                    {
                        NotNull(ref notInWallet, transaction.Outputs.Count);
                        var dest = new UtxoMetadata
                        {
                            Address = output.ScriptPubKey.GetAddressFromScriptPubKey(),
                            HashTx = transaction.GetHash(),
                            Satoshis = output.Value != null ? output.Value.Satoshi : 0,
                            Index = index
                        };
                        notInWallet.Add(dest.GetKey(), dest);
                    }

                }
                index++;
            }

            destinations = received != null
                ? notInWallet
                : null;

            amountReceived = sum;
            return received;
        }

        Dictionary<string, UtxoMetadata> ExtractOutgoingFunds(Transaction transaction, out long amountSpent)
        {
            if (transaction.IsCoinBase)
            {
                amountSpent = 0;
                return null;
            }

            List<OutPoint> prevOuts = GetPrevOuts(transaction);
            Dictionary<string, UtxoMetadata> spends = null;
            long sum = 0;

            foreach (var b in this.Metadata.Blocks.Values) // iterate ovr the large collection in outer loop (only once)
            {
                findOutPointInBlock:
                foreach (OutPoint prevOut in prevOuts)
                {
                    TransactionMetadata prevTx = b.Transactions.SingleOrDefault(x => x.HashTx == prevOut.Hash);
                    if (prevTx != null)  // prevOut tx id is in the wallet
                    {
                        var prevWalletUtxo = prevTx.Received.Values.SingleOrDefault(x => x.Index == prevOut.N);  // do we have the indexed output?
                        if (prevWalletUtxo != null)  // yes, it's a spend from this wallet
                        {
                            NotNull(ref spends, transaction.Inputs.Count); // ensure the return collection is initialized
                            spends.Add(prevWalletUtxo.GetKey(), prevWalletUtxo);  // add the spend
                            sum += prevWalletUtxo.Satoshis; // add amount

                            if (spends.Count == transaction.Inputs.Count) // we will find no more spends than inputs, quick exit
                            {
                                amountSpent = sum;
                                return spends;
                            }

                            prevOuts.Remove(prevOut); // do not search for this item any more
                            goto findOutPointInBlock; // we need a new enumerator for the shortened collection
                        }
                    }  // is the next prvOut also in this block? That's definitely possible!
                }
            }
            amountSpent = sum;
            return spends; // might be null or contain less then the tx inputs in edge cases, e.g. if an private key was removed from the wallet and no more items than the tx inputs.
        }

        static List<OutPoint> GetPrevOuts(Transaction transaction)
        {
            var prevOuts = new List<OutPoint>(transaction.Inputs.Count);
            foreach (TxIn input in transaction.Inputs)
            {
                prevOuts.Add(input.PrevOut);
            }

            return prevOuts;
        }

        static TxType GetTxType(Transaction transaction, Dictionary<string, UtxoMetadata> received, Dictionary<string, UtxoMetadata> destinations, Dictionary<string, UtxoMetadata> spent)
        {
            if (transaction.IsCoinBase)
                return TxType.Coinbase;
            if (transaction.IsCoinStake)
            {
                if (transaction.Outputs.Count == 2)
                    return TxType.CoinstakeLegacy;
                if (transaction.Outputs.Count == 3)
                    return TxType.Coinstake;
            }

            bool didReceive = received != null && received.Count > 0;
            bool didSpend = spent != null && spent.Count > 0;
            bool hasDestinations = destinations != null && destinations.Count > 0;

            if (didReceive)
            {
                if (!didSpend)
                    return TxType.Receive;

                // if we are here we also spent something
                if (!hasDestinations)
                    return TxType.WithinWallet;
                return TxType.Spend;
            }

            if (didSpend)
                return TxType.SpendWithoutChange; // we spent with no change to out wallet

            // if we are here, we neither spent or received and that should never happen for transactions that affect the wallet.
            throw new ArgumentException(
                $"{nameof(GetTxType)} cant't determine {nameof(TxType)} for transaction {transaction.GetHash()}.");
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

        #region private Methods





        bool IsAddressUsedInConfirmedTransactions(ISegWitAddress address)
        {
            // slow version
            foreach (BlockMetadata block in this.Metadata.Blocks.Values)
            {
                foreach (TransactionMetadata tx in block.Transactions)
                {
                    foreach (var utxo in tx.Received.Values)
                    {
                        if (utxo.Address == address.Address)
                            return true;
                    }

                }
            }
            return false;
        }



        void SaveMetadata()
        {
            this.Metadata.SaveX1WalletMetadataFile(this.CurrentX1WalletMetadataFilePath);
            this.logger.LogInformation("Wallet saved.");
        }



        /// <summary>
        /// Saves the tip and checkpoint from chainedHeader to the wallet file.
        /// </summary>
        /// <param name="lastBlockSynced">ChainedHeader of the last block synced.</param>
        void UpdateLastBlockSyncedAndCheckpoint(ChainedHeader lastBlockSynced)
        {
            this.Metadata.SyncedHeight = lastBlockSynced.Height;
            this.Metadata.SyncedHash = lastBlockSynced.HashBlock;

            const int minCheckpointHeight = 500;
            if (lastBlockSynced.Height > minCheckpointHeight)
            {
                var checkPoint = this.chainIndexer.GetHeader(lastBlockSynced.Height - minCheckpointHeight);
                this.Metadata.CheckpointHash = checkPoint.HashBlock;
                this.Metadata.CheckpointHeight = checkPoint.Height;
            }
            else
            {
                this.Metadata.CheckpointHash = this.network.GenesisHash;
                this.Metadata.CheckpointHeight = 0;
            }
        }

        #endregion


    }
}
