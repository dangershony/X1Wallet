using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Addresses;
using Obsidian.Features.X1Wallet.Balances;
using Obsidian.Features.X1Wallet.Blockchain;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Features.Wallet.Broadcasting;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet
{
    abstract class WalletCore
    {
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

        string stakingPassphrase;

        protected WalletCore(NodeServices nodeServices, string walletPath)
        {
            this.nodeServices = nodeServices;

            this.WalletPath = walletPath;
            this.x1WalletFile = WalletHelper.LoadX1WalletFile(walletPath);
            this.x1WalletFile.CurrentPath = walletPath;
            this.WalletName = this.x1WalletFile.WalletName;
            this.metadataPath = this.x1WalletFile.WalletName.GetX1WalletMetaDataFilepath(C.Network, nodeServices.DataFolder);
            this.metadata = WalletHelper.LoadOrCreateX1WalletMetadataFile(this.metadataPath, this.x1WalletFile, C.Network.GenesisHash);

            ScheduleSyncing();
        }


        void ScheduleSyncing()
        {
            this.isStartingUp = true;

            if (this.startupStopwatch == null)
                this.startupStopwatch = new Stopwatch();

            this.startupTimer = new Timer(_ =>
            {
                this.startupTimer.Dispose();
                try
                {
                    this.WalletSemaphore.Wait();
                    SyncWallet();
                }
                finally
                {
                    this.WalletSemaphore.Release();
                }

            }, null, 500, Timeout.Infinite);
        }

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

        void CompleteStart()
        {
            this.isStartingUp = false;
            this.startupStopwatch.Stop();
            this.startupStopwatch = null;
            this.startupTimer = null;
            SaveMetadata();
            SubscribeSignals();
        }

        void ProcessBlock(Block block, int height, uint256 hashBlock)
        {
            var walletBlock = BlockService.AnalyzeBlock(block, height, this.metadata.Blocks.Values, GetOrAddAddress);

            if (walletBlock != null)
            {
                if (!this.metadata.Blocks.TryAdd(height, walletBlock))
                {
                    MoveToBestChain();
                    SyncWallet();
                }


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

        /// <summary>
        /// It is assumed that the argument contains the header of the highest block (inclusive) where the wallet data is
        /// consistent with the right chain.
        /// This method removes all block and the transactions in them of later blocks.
        /// </summary>
        /// <param name="checkpointHeader">ChainedHeader of the checkpoint</param>
        void RemoveBlocks(ChainedHeader checkpointHeader)
        {
            var blocksAfterCheckpoint = this.metadata.Blocks.Keys.Where(x => x > checkpointHeader.Height).ToArray();
            foreach (var height in blocksAfterCheckpoint)
                this.metadata.Blocks.TryRemove(height, out var _);


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
        void ResetMetadata()
        {
            this.metadata.SyncedHash = C.Network.GenesisHash;
            this.metadata.SyncedHeight = 0;
            this.metadata.CheckpointHash = this.metadata.SyncedHash;
            this.metadata.CheckpointHeight = 0;
            this.metadata.WalletGuid = this.x1WalletFile.WalletGuid;

            SaveMetadata();
        }

        void SaveMetadata()
        {
            this.metadata.SaveX1WalletMetadataFile(this.metadataPath);
            Log.Logger.LogInformation("Wallet saved.");
        }

        ISegWitAddress GetOwnAddress(string bech32Address)
        {
            return AddressService.FindAddress(bech32Address, this.x1WalletFile);
        }

        ISegWitAddress GetOrAddAddress(string bech32Address, int blockHeight)
        {
            return AddressService.GetOrAddAddress(bech32Address, blockHeight, this.x1WalletFile);
        }

        protected abstract void SubscribeSignals();

        protected byte[] GetPassphraseChallenge()
        {
            return this.x1WalletFile.PassphraseChallenge;
        }

        protected void SetStakingPassphrase(string passphrase)
        {
            this.stakingPassphrase = passphrase;
            AddressService.TryUpdateLookAhead(passphrase, this.x1WalletFile);
            ColdStakingAddressService.EnsureDefaultColdStakingAddress(passphrase, this.x1WalletFile);
        }

        protected Balance GetBalanceCore(string matchAddress = null, AddressType matchAddressType = AddressType.MatchAll)
        {
            return BalanceService.GetBalance(this.metadata.Blocks, this.metadata.SyncedHeight,
                this.metadata.MemoryPool.Entries, GetOwnAddress, matchAddress, matchAddressType);
        }

        protected WalletDetails GetWalletDetailsCore()
        {
            var defaultCsAddress = this.GetAllColdStakingAddressesCore(0, 1).FirstOrDefault()?.Address;
            long csTotal = 0;
            long csSpendable = 0;
            long csStakable = 0;
           
            if (defaultCsAddress != null)
            {
                var balance = BalanceService.GetBalance(this.metadata.Blocks, this.metadata.SyncedHeight,
                    this.metadata.MemoryPool.Entries, GetOwnAddress,defaultCsAddress);

                csTotal = balance.Total;
                csSpendable = balance.Spendable;
                csStakable = balance.Stakable;

            }

            var defaultMsAddress = this.GetAllMultiSigAddressesCore(0, 1).FirstOrDefault()?.Address;
            long msTotal = 0;
            long msSpendable = 0;
            long msStakable = 0;

            if (defaultMsAddress != null)
            {
                var balance = BalanceService.GetBalance(this.metadata.Blocks, this.metadata.SyncedHeight,
                    this.metadata.MemoryPool.Entries, GetOwnAddress, defaultMsAddress);

                msTotal = balance.Total;
                msSpendable = balance.Spendable;
                msStakable = balance.Stakable;

            }

            var info = new WalletDetails
            {
                WalletName = this.WalletName,
                WalletFilePath = this.WalletPath,
                SyncedHeight = this.metadata.SyncedHeight,
                SyncedHash = this.metadata.SyncedHash,
                Adresses = this.x1WalletFile.PubKeyHashAddresses.Count,
                MultiSigAddresses = this.x1WalletFile.MultiSigAddresses.Count,
                ColdStakingAddresses = this.x1WalletFile.ColdStakingAddresses.Count,
                MemoryPool = this.metadata.MemoryPool,
                PassphraseChallenge = this.x1WalletFile.PassphraseChallenge.ToHexString(),
                DefaultReceiveAddress = this.GetAllPubKeyHashReceiveAddressesCore(0, 1).FirstOrDefault()?.Address,

                DefaultCSAddress = defaultCsAddress,
                CSTotal = csTotal,
                CSSpendable = csSpendable,
                CSStakable = csStakable,

                DefaultMSAddress = defaultMsAddress,
                MSTotal = msTotal,
                MSSpendable = msSpendable,
                MSStakable = msStakable
            };

            info.Balance = GetBalanceCore();

            return info;
        }

        protected HistoryInfo GetHistoryInfoCore(HistoryRequest historyRequest)
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

        protected void SyncWallet()
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

            if (this.isStartingUp)
                CompleteStart();
        }

        protected int GetSyncedHeight()
        {
            return this.metadata.SyncedHeight;
        }

        protected uint256 GetSyncedHash()
        {
            return this.metadata.SyncedHash;
        }

        protected void TransactionBroadcastEntryChanged(TransactionBroadcastEntry broadcastEntry)
        {
            var memoryPoolEntry = MemoryPoolService.GetMemoryPoolEntry(broadcastEntry.Transaction.GetHash(), this.metadata.MemoryPool.Entries);

            if (memoryPoolEntry == null)
            {
                TransactionMetadata walletTransaction = BlockService.AnalyzeTransaction(broadcastEntry.Transaction, int.MaxValue, this.metadata.Blocks.Values, GetOrAddAddress);
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

        protected void ReceiveTransactionFromMemoryPool(Transaction transaction)
        {
            var walletTransaction = BlockService.AnalyzeTransaction(transaction, int.MaxValue, this.metadata.Blocks.Values, GetOrAddAddress);
            if (walletTransaction != null)
            {
                var entry = MemoryPoolService.CreateMemoryPoolEntry(walletTransaction, null);
                this.metadata.MemoryPool.Entries.Add(entry);
            }
            SaveMetadata();
        }

        protected PubKeyHashAddress[] GetAllPubKeyHashReceiveAddressesCore(int skip, int? take)
        {
            return AddressService.GetAllPubKeyHashReceiveAddresses(skip, take, this.x1WalletFile);
        }

        protected ColdStakingAddress[] GetAllColdStakingAddressesCore(int skip, int? take)
        {
            return ColdStakingAddressService.GetAllColdStakingAddresses(skip, take, this.x1WalletFile);
        }

        protected MultiSigAddress[] GetAllMultiSigAddressesCore(int skip, int? take)
        {
            return MultiSigAddressService.GetAllMultiSigAddresses(skip, take, this.x1WalletFile);
        }

        protected PubKeyHashAddress CreateNewReceiveAddressCore(string label, string passphrase)
        {
            if (!IsWalletInPerfectShape())
                throw new X1WalletException("Please wait till the wallet's last received block time matches current time.");

            if (string.IsNullOrWhiteSpace(label))
                throw new X1WalletException("You need to supply a label for your new receive address");

            if (label.Length > 27)
                throw new X1WalletException("The maximum length for the label is 27 characters.");

            if (!AddressService.IsLabelUnique(label, this.x1WalletFile))
                throw new X1WalletException($"The label '{label}' is already in use.");

            if (string.IsNullOrWhiteSpace(passphrase))
                throw new X1WalletException("The wallet passphrase is required.");

            var newAddress = AddressService.CreateAndInsertNewReceiveAddress(label, passphrase, this.x1WalletFile);
            Log.Logger.LogInformation(
                $"Created and saved new receive address '{newAddress.Address}' with label '{newAddress.Label}'");
            return newAddress;
        }

        protected PubKeyHashAddress GetUnusedChangeAddressCore(string passphrase, bool isDummy)
        {
            if (passphrase == null)
                passphrase = this.stakingPassphrase;

            AddressService.TryUpdateLookAhead(passphrase, this.x1WalletFile);

            return AddressService.GetChangeAddress(passphrase, isDummy, this.x1WalletFile);
        }

        protected bool IsLabelUniqueCore(string label)
        {
            return AddressService.IsLabelUnique(label, this.x1WalletFile);
        }

        bool IsWalletInPerfectShape()
        {
            // should only return true if wallet is not only synced with the node,
            // but also the last block is not older than a few minutes relative to real time,
            // so that we are as sure as we can be we discovered all used addresses from the chain.
            return true;
        }
    }
}
