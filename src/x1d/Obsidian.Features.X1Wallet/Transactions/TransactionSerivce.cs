using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Balances;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models.Api;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Transactions
{
    sealed class TransactionService
    {
        const int SignatureVirtualSize = 28;
        const int ChangeOutputVirtualSize = 31;
        const int MinimumVirtualSize = 86;  // one input, one output, without signature and change output

        readonly long minimumPossibleFee;
        readonly long dustThreshold;

        readonly ILogger logger;
        readonly Network network;
        readonly WalletManagerFactory walletManagerFactory;
        readonly string walletName;
        readonly long feePer1000VBytes;

        public TransactionService(
            ILoggerFactory loggerFactory,
            WalletManagerFactory walletManagerFactory, string walletName,
            Network network)
        {
            this.network = network;
            this.walletManagerFactory = walletManagerFactory;
            this.walletName = walletName;
            this.logger = loggerFactory.CreateLogger(GetType().FullName);
            this.feePer1000VBytes = Math.Max(network.MinTxFee, network.MinRelayTxFee);
            this.minimumPossibleFee = (MinimumVirtualSize + SignatureVirtualSize + ChangeOutputVirtualSize) * this.feePer1000VBytes / 1000;
            this.dustThreshold = this.minimumPossibleFee * 5;
        }

        public TransactionResponse BuildTransaction(List<Recipient> recipients, bool sign, string passphrase = null, List<Burn> burns = null)
        {
            var tx = this.network.CreateTransaction();

            // add recipients
            long recipientsAmount = 0;
            foreach (Recipient recipient in recipients)
            {
                recipientsAmount += recipient.Amount;
                tx.Outputs.Add(new TxOut(recipient.Amount, recipient.Address.GetScriptPubKey()));
            }

            // add burns
            long burnAmount = 0;
            if (burns != null)
                foreach (Burn burn in burns)
                {
                    burnAmount += burn.Amount;
                    tx.Outputs.Add(new TxOut(burn.Amount, TxNullDataTemplate.Instance.GenerateScriptPubKey(burn.Data)));
                }

            long totalSendAmount = recipientsAmount + burnAmount;


            // calculate size, fee and change amount
            long addedFee = this.minimumPossibleFee;

            SegWitCoin[] selectedCoins;

            while (true)
            {
                tx.Inputs.Clear();

                int selectedCount = 0;
                long selectedAmount = 0;

                selectedCoins = AddCoins(totalSendAmount, addedFee).ToArray();

                foreach (var c in selectedCoins)
                {
                    selectedCount++;
                    selectedAmount += c.UtxoValue;
                    tx.Inputs.Add(new TxIn(new OutPoint(c.UtxoTxHash, c.UtxoTxN)));
                }

                var virtualSize = tx.GetVirtualSize() + selectedCount * SignatureVirtualSize + ChangeOutputVirtualSize;

                long requiredFee = virtualSize * this.feePer1000VBytes / 1000;

                if (addedFee == requiredFee)
                {
                    var change = selectedAmount - totalSendAmount - addedFee;
                    if (change > this.dustThreshold)
                    {
                        TxOut changeOutput = GetOutputForChange(passphrase, !sign);
                        changeOutput.Value = change;
                        tx.Outputs.Add(changeOutput);
                    }
                    break;
                }

                addedFee = requiredFee;
            }

            // signing
            if (sign)
            {
                var keys = DecryptKeys(selectedCoins, passphrase);
                SigningService.SignInputs(tx, keys, selectedCoins);
            }

            var response = new TransactionResponse
            {
                Transaction = tx,
                Hex = tx.ToHex(),
                Fee = addedFee,
                VirtualSize = tx.GetVirtualSize(),
                SerializedSize = tx.GetSerializedSize(),
                TransactionId = tx.GetHash(),
                BroadcastState = BroadcastState.NotSet
            };

            return response;
        }

        IEnumerable<SegWitCoin> AddCoins(long totalAmountToSend, long fee)
        {
            long totalToSelect = totalAmountToSend + fee;

            Balance balance;
            using (var walletContext = GetWalletContext())
            {
                balance = walletContext.WalletManager.GetBalance(null, AddressType.PubKeyHash);
            }

            if (balance.Spendable < totalToSelect)
                throw new X1WalletException(System.Net.HttpStatusCode.BadRequest,
                    $"Required are at least {totalToSelect}, but spendable is only {balance.Spendable}");

            long selectedAmount = 0;
            var selectedCoins = new List<SegWitCoin>();
            foreach (var coin in balance.SpendableCoins.Values)
            {
                if (selectedAmount < totalToSelect)
                {
                    selectedCoins.Add(coin);
                    selectedAmount += coin.UtxoValue;
                }
                else
                    return selectedCoins;
            }
            return selectedCoins;
        }

        TxOut GetOutputForChange(string passphrase, bool isDummy)
        {
            using (var walletContext = GetWalletContext())
            {
                var changeAddress = walletContext.WalletManager.GetUnusedChangeAddress(passphrase, isDummy);
                return new TxOut(0, changeAddress.GetScriptPubKey());
            }
        }

        static Key[] DecryptKeys(SegWitCoin[] selectedCoins, string passphrase)
        {
            var keys = new Key[selectedCoins.Length];
            for (var i = 0; i < keys.Length; i++)
                keys[i] = selectedCoins[i].GetPrivateKey(passphrase);
            return keys;
        }

        WalletContext GetWalletContext()
        {
            return this.walletManagerFactory.AutoLoad(this.walletName);
        }
    }
}
