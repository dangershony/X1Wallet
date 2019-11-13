using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Api;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Features.ColdStaking;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Transactions
{
    sealed class ColdStakingTransactionService
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

        public ColdStakingTransactionService(
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

        public void Setup(string passphrase = null)
        {
            var csAddress = EnsureColdStakingAddress(passphrase);

            // I have received 100_000 in my wallet in this address
            Key myBudgetKey = new Key();
            PubKey myBudgetPubKey = myBudgetKey.PubKey.Compress();
            Transaction received = this.network.CreateTransaction();
            received.Outputs.Add(new TxOut(Money.Coins(100_000), myBudgetPubKey.WitHash.ScriptPubKey));
            List<Coin> myBudgetCoins = received.Outputs.AsCoins().ToList();


            var builder = new TransactionBuilder(this.network);


            throw new NotImplementedException();
            Transaction csSetupTx =
                builder
                    .AddCoins(myBudgetCoins)
             //       .Send(new Script(csAddress.ScriptPubKey), Money.Coins(90_000)) // 90_000 to cold staking script address
                    .SetChange(myBudgetPubKey.WitHash.ScriptPubKey) // 10_000 back to original source
                    .SendFees(Money.Satoshis(500))
                    .AddKeys(myBudgetKey)
                    .BuildTransaction(sign: true);

            bool isVerifyPassing = builder.Verify(csSetupTx, out var errors);
            foreach (var err in errors)
                this.logger.LogError(err.ToString());

            bool hasEmptyScriptSig = csSetupTx.Inputs.All(i => i.ScriptSig.Length == 0);

            this.logger.LogInformation($"isVerifyPassing: {isVerifyPassing}");
            this.logger.LogInformation($"hasWitness: {csSetupTx.HasWitness}");
            this.logger.LogInformation($"hasEmptyScriptSig: {hasEmptyScriptSig}");
            this.logger.LogInformation(csSetupTx.ToString());

            Debug.Assert(isVerifyPassing);
            Debug.Assert(csSetupTx.HasWitness);
            Debug.Assert(hasEmptyScriptSig);
        }

        ColdStakingAddress EnsureColdStakingAddress(string passphrase)
        {
            using (var context = GetWalletContext())
            {
                return context.WalletManager.EnsureColdStakingAddress(passphrase);
            }
        }

        public ColdStakingTransactionResponse BuildTransaction(string sourceMultiSigAddress, List<Recipient> recipients, bool sign, string passphrase = null, List<Burn> burns = null)
        {

            var builder = new TransactionBuilder(this.network);

            foreach (var r in recipients)
            {
                Script scriptPubKey = r.Address.Length == AddressHelper.Bech32PubKeyAddressLenght
                    ? r.Address.ScriptPubKeyFromBech32Safe()
                    : r.Address.ScriptPubKeyFromBech32ScriptAddressSafe();
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

            
            var response = new ColdStakingTransactionResponse
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

            IReadOnlyList<StakingCoin> budget;
            Balance balance;
            using (var walletContext = GetWalletContext())
            {
                budget = walletContext.WalletManager.GetMultiSigBudget(out balance, sourceMultiSigAddress);
            }

            ownPrivateKey = null;
            Script redeemScript = null;
            var scriptCoins = new List<ScriptCoin>();
            foreach (var stakingCoin in budget)
            {
                if (redeemScript == null)
                {
                    redeemScript = stakingCoin.RedeemScript;
                    ownPrivateKey = DecryptKeys(new[] { stakingCoin }, passphrase)[0];
                }
                else
                {
                    if (stakingCoin.RedeemScript != redeemScript)
                        throw new InvalidOperationException("All redeem scripts must be identical.");
                }
                var scriptCoin = stakingCoin.ToScriptCoin(stakingCoin.RedeemScript);
                scriptCoins.Add(scriptCoin);
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



        static Key[] DecryptKeys(StakingCoin[] selectedCoins, string passphrase)
        {
            var keys = new Key[selectedCoins.Length];
            for (var i = 0; i < keys.Length; i++)
                keys[i] = new Key(VCL.DecryptWithPassphrase(passphrase, selectedCoins[i].EncryptedPrivateKey));
            return keys;
        }

        WalletContext GetWalletContext()
        {
            return this.walletManagerFactory.GetWalletContext(this.walletName);
        }


    }
}
