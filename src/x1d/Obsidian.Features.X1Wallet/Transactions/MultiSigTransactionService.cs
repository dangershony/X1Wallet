using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Api;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Transactions
{
    sealed class MultiSigTransactionService
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

        public MultiSigTransactionService(
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

        public MultiSigTransactionResponse BuildTransaction(string sourceMultiSigAddress, List<Recipient> recipients, bool sign, string passphrase = null, List<Burn> burns = null)
        {

            var builder = new TransactionBuilder(this.network);

            foreach (var r in recipients)
            {
                Script scriptPubKey = r.Address.GetScriptPubKey();
                builder.Send(scriptPubKey, r.Amount);
            }

            if (burns != null)
                foreach (Burn burn in burns)
                {
                    builder.Send(TxNullDataTemplate.Instance.GenerateScriptPubKey(burn.Data), burn.Amount);
                }

            var scriptCoins = GetAllCoinsForMultiSigAccount(sourceMultiSigAddress, passphrase, out Script scriptPubKeyForChange, out Key ownPrivateKey);
            builder.AddCoins(scriptCoins);
            builder.AddKeys(ownPrivateKey);
            builder.SetChange(scriptPubKeyForChange);

            var estimatedFee = builder.EstimateFees(new FeeRate(this.feePer1000VBytes)) * 5;  // TODO
            builder.SendFees(estimatedFee);

            var transaction = builder.BuildTransaction(sign);

            if (sign)
            {
                transaction = builder.CombineSignatures(transaction);
            }


            bool isVerifyPassing = builder.Verify(transaction, out var errors);
            foreach (var err in errors)
                this.logger.LogError(err.ToString());

            this.logger.LogInformation($"isVerifyPassing: {isVerifyPassing}");
            this.logger.LogInformation(transaction.ToString());

            
            var response = new MultiSigTransactionResponse
            {
                Transaction = transaction,
                Hex = transaction.ToHex(),
                Fee = estimatedFee,
                VirtualSize = transaction.GetVirtualSize(),
                SerializedSize = transaction.GetSerializedSize(),
                TransactionId = transaction.GetHash(),
                BroadcastState = BroadcastState.NotSet
            };

            return response;
        }



        IEnumerable<ScriptCoin> GetAllCoinsForMultiSigAccount(string sourceMultiSigAddress, string passphrase, out Script scriptPubKeyForChange, out Key ownPrivateKey)
        {
            IReadOnlyList<SegWitCoin> budget;
            Balance balance;
            using (var walletContext = GetWalletContext())
            {
                budget = walletContext.WalletManager.GetBudget(out balance, matchAddress: sourceMultiSigAddress,
                    matchAddressType: AddressType.MultiSig);
            }

            ownPrivateKey = null;
            Script redeemScript = null;
            var scriptCoins = new List<ScriptCoin>();
            foreach (var segWitCoin in budget)
            {
                var multiSigAddress = (MultiSigAddress) segWitCoin.SegWitAddress;
                var scriptCoin = segWitCoin.ToCoin().ToScriptCoin(multiSigAddress.GetRedeemScript());
               
                scriptCoins.Add(scriptCoin);
                // TODO
            }

            if (scriptCoins.Count == 0)
            {
                scriptPubKeyForChange = null;
                return scriptCoins;
            }

            Debug.Assert(redeemScript != null, nameof(redeemScript) + " != null");
            string bech32ScriptAddress = redeemScript.WitHash.GetAddress(this.network).ToString();
            if (bech32ScriptAddress != sourceMultiSigAddress)
                throw new InvalidOperationException("Address mismatch.");

            // In script hash payments, we refer to the hash of the Redeem Script as the ScriptPubKey.
            scriptPubKeyForChange = redeemScript.WitHash.ScriptPubKey;

            return scriptCoins;
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
