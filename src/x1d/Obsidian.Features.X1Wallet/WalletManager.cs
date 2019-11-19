using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Addresses;
using Obsidian.Features.X1Wallet.Balances;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Api.Responses;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.EventBus;
using Stratis.Bitcoin.EventBus.CoreEvents;
using Stratis.Bitcoin.Features.ColdStaking;
using Stratis.Bitcoin.Features.Wallet.Broadcasting;
using Stratis.Bitcoin.Utilities;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet
{
    sealed class WalletManager : WalletCore, IDisposable
    {
        readonly NodeServices nodeServices;
       
        SubscriptionToken blockConnectedSubscription;
        SubscriptionToken transactionReceivedSubscription;
        StakingService stakingService;

        public int SyncedHeight => base.GetSyncedHeight();
        public uint256 SyncedHash => base.GetSyncedHash();

        public WalletManager(NodeServices nodeServices, string walletPath) : base(nodeServices, walletPath)
        {
            this.nodeServices = nodeServices;
        }

        protected override void SubscribeSignals()
        {
            this.nodeServices.BroadcasterManager.TransactionStateChanged += OnTransactionStateChanged;
            this.blockConnectedSubscription = this.nodeServices.Signals.Subscribe<BlockConnected>(OnBlockConnected);
            this.transactionReceivedSubscription = this.nodeServices.Signals.Subscribe<TransactionReceived>(OnTransactionReceived);
        }

        void OnBlockConnected(BlockConnected blockConnected)
        {
            base.SyncWallet();
        }

        void OnTransactionReceived(TransactionReceived transactionReceived)
        {
            try
            {
                this.WalletSemaphore.Wait();

                base.ReceiveTransactionFromMemoryPool(transactionReceived.ReceivedTransaction);
            }
            finally
            {
                this.WalletSemaphore.Release();
            }
        }

        void OnTransactionStateChanged(object sender, TransactionBroadcastEntry broadcastEntry)
        {
            if (broadcastEntry.State == State.CantBroadcast)
                return;

            try
            {
                this.WalletSemaphore.Wait();

                base.TransactionBroadcastEntryChanged(broadcastEntry);
            }
            finally
            {
                this.WalletSemaphore.Release();
            }
        }

        internal LoadWalletResponse LoadWallet()
        {
            return new LoadWalletResponse { PassphraseChallenge = base.GetPassphraseChallenge().ToHexString() };
        }


        public Balance GetBalance(string matchAddress = null, AddressType matchAddressType = AddressType.MatchAll)
        {
            return base.GetBalanceCore(matchAddress, matchAddressType);
        }

        public WalletDetails GetWalletDetails()
        {
            var walletDetails = base.GetWalletDetailsCore();
            walletDetails.StakingInfo = GetStakingInfo();
            walletDetails.Balance = base.GetBalanceCore();

            return walletDetails;
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
            return base.GetHistoryInfoCore(historyRequest);
        }

        internal PubKeyHashAddress[] GetAllPubKeyHashReceiveAddresses(int skip, int? take)
        {
            return base.GetAllPubKeyHashReceiveAddressesCore(skip, take);
        }

        internal PubKeyHashAddress CreateReceiveAddress(string label, string passphrase)
        {
            Guard.NotNull(label, nameof(label));
            return base.CreateNewReceiveAddressCore(label, passphrase);
        }

        internal PubKeyHashAddress GetUnusedChangeAddress(string passphrase, bool isDummy)
        {
            return base.GetUnusedChangeAddressCore(passphrase, isDummy);
        }

        public void StartStaking(string passphrase)
        {
            Guard.NotNull(passphrase, nameof(passphrase));

            if (VCL.DecryptWithPassphrase(passphrase, base.GetPassphraseChallenge()) == null)
                throw new X1WalletException(HttpStatusCode.Unauthorized, "The passphrase is not correct.");

            base.SetStakingPassphrase(passphrase);
           

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

        internal ImportKeysResponse ImportKeys(ImportKeysRequest importKeysRequest)
        {
            // TODO
            //return ImportExportService.ImportKeys(importKeysRequest, this.x1WalletFile.PassphraseChallenge);
            return null;
        }

        internal ExportKeysResponse ExportKeys(ExportKeysRequest exportKeysRequest)
        {
            // TODO
            //return ImportExportService.ExportKeys(exportKeysRequest, this.x1WalletFile.PubKeyHashAddresses.Values);
            return null;
        }

        internal void RepairWallet(RepairRequest repairRequest)
        {
            throw new NotImplementedException();
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
    }
}
