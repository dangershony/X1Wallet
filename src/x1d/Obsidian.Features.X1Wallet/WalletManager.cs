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
        public readonly string WalletPath;
        public readonly string WalletName;

        readonly NodeServices nodeServices;
        readonly string metadataPath;
        readonly X1WalletFile x1WalletFile;
        readonly X1WalletMetadataFile metadata;

        bool isStartingUp;
        Stopwatch startupStopwatch;
        long startupDuration;
        Timer startupTimer;
        SubscriptionToken blockConnectedSubscription;
        SubscriptionToken transactionReceivedSubscription;
        StakingService stakingService;

        public WalletManager(NodeServices nodeServices, string walletPath)
        {
            this.nodeServices = nodeServices;

            this.WalletPath = walletPath;
            this.x1WalletFile = WalletHelper.LoadX1WalletFile(walletPath);
            this.WalletName = this.x1WalletFile.WalletName;
            this.metadataPath = this.x1WalletFile.WalletName.GetX1WalletMetaDataFilepath(C.Network, nodeServices.DataFolder);
            this.metadata = WalletHelper.LoadOrCreateX1WalletMetadataFile(this.metadataPath, this.x1WalletFile, C.Network.GenesisHash);

            ScheduleSyncing();
        }

        public void Dispose()
        {
            StopStaking();

            this.nodeServices.BroadcasterManager.TransactionStateChanged -= OnTransactionStateChanged;

            if (this.transactionReceivedSubscription != null)
                this.nodeServices.Signals.Unsubscribe(this.transactionReceivedSubscription);

            if (this.blockConnectedSubscription != null)
                this.nodeServices.Signals.Unsubscribe(this.blockConnectedSubscription);
        }



        #endregion

        #region public get-only properties





        public int WalletLastBlockSyncedHeight => this.metadata.SyncedHeight;

        public uint256 WalletLastBlockSyncedHash => this.metadata.SyncedHash;

        #endregion

        #region event handlers

        void OnTransactionStateChanged(object sender, TransactionBroadcastEntry broadcastEntry)
        {
            if (broadcastEntry.State == State.CantBroadcast)
                return;

            try
            {
                this.WalletSemaphore.Wait();

                var memoryPoolEntry = MemoryPoolService.GetMemoryPoolEntry(broadcastEntry.Transaction.GetHash(), this.metadata.MemoryPool.Entries);

                if (memoryPoolEntry == null)
                {
                    TransactionMetadata walletTransaction = BlockService.AnalyzeTransaction(broadcastEntry.Transaction, this.metadata.Blocks.Values, GetOwnAddress);
                    if (walletTransaction != null)
                    {
                        var entry = MemoryPoolService.CreateMemoryPoolEntry(walletTransaction, broadcastEntry);
                        this.metadata.MemoryPool.Entries.Add(entry);
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

      

        void OnTransactionReceived(TransactionReceived transactionReceived)
        {
            try
            {
                this.WalletSemaphore.Wait();

                var walletTransaction = BlockService.AnalyzeTransaction(transactionReceived.ReceivedTransaction, this.metadata.Blocks.Values, GetOwnAddress);
                if (walletTransaction != null)
                {
                    var entry = MemoryPoolService.CreateMemoryPoolEntry(walletTransaction, null);
                    this.metadata.MemoryPool.Entries.Add(entry);
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

        void SyncWallet()
        {
            if (this.nodeServices.InitialBlockDownloadState.IsInitialBlockDownload())
            {
                Log.Logger.LogInformation("Wallet is waiting for IBD to complete.");
                ScheduleSyncing();
                return;
            }

            if (this.nodeServices.ChainIndexer.Tip == null || this.nodeServices.ChainIndexer.Tip.HashBlock == null)
            {
                Log.Logger.LogInformation("Waiting for the ChainIndexer to initialize.");
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


                Log.Logger.LogInformation(
                    $"Wallet {this.WalletName} is at block {this.metadata.SyncedHeight}, catching up, {TimeSpan.FromMilliseconds(this.startupDuration).Duration()} elapsed.");

                while (this.nodeServices.ChainIndexer.Tip.Height > this.metadata.SyncedHeight)
                {
                    // this can take a long time, so watch for cancellation
                    if (this.nodeServices.NodeLifetime.ApplicationStopping.IsCancellationRequested)
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

                    var nextBlockForWalletHeight = this.metadata.SyncedHeight + 1;
                    ChainedHeader nextBlockForWalletHeader = this.nodeServices.ChainIndexer.GetHeader(nextBlockForWalletHeight);
                    Block nextBlockForWallet = this.nodeServices.BlockStore.GetBlock(nextBlockForWalletHeader.HashBlock);
                    ProcessBlock(nextBlockForWallet, nextBlockForWalletHeader.Height, nextBlockForWallet.GetHash());
                }
            }
            catch (Exception e)
            {
                Log.Logger.LogError($"{nameof(SyncWallet)}: {e.Message}");
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


            this.nodeServices.BroadcasterManager.TransactionStateChanged += OnTransactionStateChanged;
            this.blockConnectedSubscription = this.nodeServices.Signals.Subscribe<BlockConnected>(OnBlockConnected);
            this.transactionReceivedSubscription = this.nodeServices.Signals.Subscribe<TransactionReceived>(OnTransactionReceived);
        }



        /// <summary>
        /// Checks if the wallet is on the right chain.
        /// </summary>
        /// <returns>true, if on the right chain.</returns>
        bool IsOnBestChain()
        {
            bool isOnBestChain;
            if (this.metadata.SyncedHeight == 0 || this.metadata.SyncedHash.IsDefaultBlockHash())
            {
                // if the height is 0, we cannot be on the wrong chain
                ResetMetadata();
                isOnBestChain = true;

            }
            else
            {
                // check if the wallet tip hash is in the current consensus chain
                isOnBestChain = this.nodeServices.ChainIndexer.GetHeader(this.metadata.SyncedHash) != null;
            }

            return isOnBestChain;
        }

        /// <summary>
        /// If IsOnBestChain returns false, we need to fix this by removing the fork blocks from the wallet.
        /// </summary>
        void MoveToBestChain()
        {
            ChainedHeader checkpointHeader = null;
            if (!this.metadata.CheckpointHash.IsDefaultBlockHash())
            {
                var header = this.nodeServices.ChainIndexer.GetHeader(this.metadata.CheckpointHash);
                if (header != null && this.metadata.CheckpointHeight == header.Height)
                    checkpointHeader = header;  // the checkpoint header is in the correct chain and the the checkpoint height in the wallet is consistent
            }
            if (checkpointHeader != null && this.nodeServices.ChainIndexer.Tip.Height - checkpointHeader.Height > C.Network.Consensus.MaxReorgLength)  // also check the checkpoint is not newer than it should be
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
            this.metadata.SyncedHeight = height;
            this.metadata.SyncedHash = hashBlock;

            const int minCheckpointHeight = 500;
            if (height > minCheckpointHeight)
            {
                var checkPoint = this.nodeServices.ChainIndexer.GetHeader(height - minCheckpointHeight);
                this.metadata.CheckpointHash = checkPoint.HashBlock;
                this.metadata.CheckpointHeight = checkPoint.Height;
            }
            else
            {
                this.metadata.CheckpointHash = C.Network.GenesisHash;
                this.metadata.CheckpointHeight = 0;
            }
        }

        void ProcessBlock(Block block, int height, uint256 hashBlock)
        {
            var walletBlock = BlockService.AnalyzeBlock(block, this.metadata.Blocks.Values, GetOwnAddress);

            if (walletBlock != null)
            {
                this.metadata.Blocks.Add(height, walletBlock);
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
                this.metadata.MemoryPool.Entries.Remove(new MemoryPoolEntry { Transaction = transActionToMigrate });
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
            var blocksAfterCheckpoint = this.metadata.Blocks.Keys.Where(x => x > checkpointHeader.Height).ToArray();
            foreach (var height in blocksAfterCheckpoint)
                this.metadata.Blocks.Remove(height);


            // Update last block synced height
            this.metadata.SyncedHeight = checkpointHeader.Height;
            this.metadata.SyncedHash = checkpointHeader.HashBlock;
            this.metadata.CheckpointHeight = checkpointHeader.Height;
            this.metadata.CheckpointHash = checkpointHeader.HashBlock;
            SaveMetadata();
        }

        /// <summary>
        /// Clears and initializes the wallet Metadata file, and sets heights to 0 and the hashes to null,
        /// and saves the Metadata file, effectively updating it to the latest version.
        /// </summary>
        internal void ResetMetadata()
        {
            this.metadata.SyncedHash = C.Network.GenesisHash;
            this.metadata.SyncedHeight = 0;
            this.metadata.CheckpointHash = this.metadata.SyncedHash;
            this.metadata.CheckpointHeight = 0;
            this.metadata.Blocks = new Dictionary<int, BlockMetadata>();
            this.metadata.WalletGuid = this.x1WalletFile.WalletGuid;

            SaveMetadata();
        }

        #endregion


        internal LoadWalletResponse LoadWallet()
        {
            return new LoadWalletResponse { PassphraseChallenge = this.x1WalletFile.PassphraseChallenge.ToHexString() };
        }

        internal string EnsureDummyMultiSig1Of2Address()
        {
            var passphrase = "passwordpassword";

            if (this.x1WalletFile.HdSeed == null)
            {
                var hdBytes = new byte[32];
                var Rng = new RNGCryptoServiceProvider();
                Rng.GetBytes(hdBytes);
                var wl = Wordlist.English;
                var mnemonic = new Mnemonic(wl, hdBytes);
                byte[] hdSeed = mnemonic.DeriveSeed("");
                this.x1WalletFile.HdSeed = VCL.EncryptWithPassphrase(passphrase, hdSeed);
            }


            var seed = VCL.DecryptWithPassphrase(passphrase, this.x1WalletFile.HdSeed);

            // own key
            KeyMaterial myKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, C.Network.Consensus.CoinType, AddressType.MultiSig, 0, 0);
            PubKey myPubKey = myKeyMaterial.GetKey(passphrase).PubKey.Compress();

            // other Key
            KeyMaterial otherKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, C.Network.Consensus.CoinType, AddressType.MultiSig, 0, 1);
            var otherPubKey = otherKeyMaterial.GetKey(passphrase).PubKey.Compress();

            // The redeem script looks like:
            // 1 03fad6426522dbda5c5a9f8cab24a54ccc374517ad8790bf7e5a14308afc1bf77b 0340ecf2e20978075a49369e35269ecf0651d2f48061ebbf918f3eb1964051f65c 2 OP_CHECKMULTISIG
            Script redeemScript = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(1, myPubKey, otherPubKey);

            // The address looks like:
            // odx1qvar8r29r8llzj53q5utmcewpju59263h38250ws33lp2q45lmalqg5lmdd
            string bech32ScriptAddress = redeemScript.WitHash.GetAddress(C.Network).ToString();

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

            if (this.x1WalletFile.ColdStakingAddresses != null && this.x1WalletFile.ColdStakingAddresses.Count > 0)
            {
                return this.x1WalletFile.ColdStakingAddresses.Values.First();
            }

            this.x1WalletFile.ColdStakingAddresses = new Dictionary<string, ColdStakingAddress>();

            var seed = VCL.DecryptWithPassphrase(passphrase, this.x1WalletFile.HdSeed);

            KeyMaterial coldKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, C.Network.Consensus.CoinType, AddressType.ColdStakingCold, 0, 0);
            PubKey coldPubKey = coldKeyMaterial.GetKey(passphrase).PubKey.Compress();

            KeyMaterial hotKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, C.Network.Consensus.CoinType, AddressType.ColdStakingHot, 0, 0);
            PubKey hotPubKey = hotKeyMaterial.GetKey(passphrase).PubKey.Compress();

            Script csRedeemScript = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKey.WitHash.AsKeyId(), coldPubKey.WitHash.AsKeyId());

            // In P2SH payments, we refer to the hash of the Redeem Script as the scriptPubKey. It looks like:
            // 0 674671a8a33ffe295220a717bc65c19728556a3789d547ba118fc2a0569fdf7e
            Script scriptPubKey = csRedeemScript.WitHash.ScriptPubKey;

            string bech32ScriptAddress = csRedeemScript.WitHash.GetAddress(C.Network).ToString();



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

        internal IReadOnlyList<PubKeyHashAddress> GetPubKeyHashAddresses(int change, int? take)
        {
            return AddressService.GetPubKeyHashAddresses(change, take, this.x1WalletFile);
        }


        #region public methods




        public Balance GetBalance(string matchAddress = null, AddressType matchAddressType = AddressType.MatchAll)
        {
            return BalanceService.GetBalance(this.metadata.Blocks, this.metadata.SyncedHeight,
                this.metadata.MemoryPool.Entries, GetOwnAddress, matchAddress, matchAddressType);
        }

        ISegWitAddress GetOwnAddress(string bech32Address)
        {
            return this.x1WalletFile.FindAddress(bech32Address);
        }

        internal WalletDetails GetWalletDetails()
        {
            var info = new WalletDetails
            {
                WalletName = this.WalletName,
                WalletFilePath = this.WalletPath,
                SyncedHeight = this.metadata.SyncedHeight,
                SyncedHash = this.metadata.SyncedHash,
                Adresses = this.x1WalletFile.PubKeyHashAddresses.Count,
                MultiSigAddresses = this.x1WalletFile.MultiSigAddresses.Count,
                ColdStakingAddresses = this.x1WalletFile.ColdStakingAddresses.Count,
                StakingInfo = GetStakingInfo(),
                MemoryPool = this.metadata.MemoryPool,
                PassphraseChallenge = this.x1WalletFile.PassphraseChallenge.ToHexString()

            };

            try
            {
                var unusedReceiveAddress = AddressService.GetUnusedReceiveAddress(this.x1WalletFile);
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
            foreach (var item in this.metadata.MemoryPool.Entries.OrderByDescending(x => x.TransactionTime))
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
                var bi = new HistoryInfo.BlockInfo { Height = int.MaxValue, Time = this.metadata.MemoryPool.Entries.Max(x => x.TransactionTime), Transactions = unconfirmed };
                historyInfo.Blocks.Add(bi);
            }


            foreach ((int height, BlockMetadata block) in this.metadata.Blocks.Reverse())
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

            if (VCL.DecryptWithPassphrase(passphrase, this.x1WalletFile.PassphraseChallenge) == null)
                throw new X1WalletException(HttpStatusCode.Unauthorized, "The passphrase is not correct.");

            if (!C.Network.Consensus.IsProofOfStake)
                throw new X1WalletException(HttpStatusCode.BadRequest, "Staking requires a Proof-of-Stake consensus.");

            if (this.nodeServices.TimeSyncBehaviorState.IsSystemTimeOutOfSync)
            {
                string errorMessage = "Staking cannot start, your system time does not match that of other nodes on the network." + Environment.NewLine
                                                                                                                                  + "Please adjust your system time and restart the node.";
                Log.Logger.LogError(errorMessage);
                throw new X1WalletException(HttpStatusCode.InternalServerError, errorMessage);
            }

            if (this.stakingService == null)
            {
                this.stakingService = new StakingService(this, passphrase, this.nodeServices);
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
            this.metadata.SaveX1WalletMetadataFile(this.metadataPath);
            Log.Logger.LogInformation("Wallet saved.");
        }


        public PubKeyHashAddress GetUnusedReceiveAddress()
        {
            return AddressService.GetUnusedReceiveAddress(this.x1WalletFile);
        }

        internal ImportKeysResponse ImportKeys(ImportKeysRequest importKeysRequest)
        {
            // TODO
            return ImportExportService.ImportKeys(importKeysRequest, this.x1WalletFile.PassphraseChallenge);
        }

        internal ExportKeysResponse ExportKeys(ExportKeysRequest exportKeysRequest)
        {
            // TODO
            return ImportExportService.ExportKeys(exportKeysRequest, this.x1WalletFile.PubKeyHashAddresses.Values);
        }

        internal IEnumerable<PubKeyHashAddress> GetReceiveAddresses(int count, bool v, string passphrase)
        {
            return AddressService.GetReceiveAddresses(count, v, passphrase, this.x1WalletFile);
        }

        internal PubKeyHashAddress GetUnusedChangeAddress(string passphrase, bool isDummy)
        {
            return AddressService.GetUnusedChangeAddress(passphrase, isDummy, this.x1WalletFile);
        }
    }
}
