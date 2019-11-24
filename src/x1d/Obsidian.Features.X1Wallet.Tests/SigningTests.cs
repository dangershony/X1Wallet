using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin;
using Obsidian.Features.X1Wallet.Addresses;
using Obsidian.Features.X1Wallet.Balances;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tests.Fakes;
using Obsidian.Features.X1Wallet.Tools;
using Obsidian.Features.X1Wallet.Transactions;
using Obsidian.Networks.ObsidianX;
using Stratis.Bitcoin.Features.ColdStaking;
using Stratis.Bitcoin.Primitives;
using VisualCrypt.VisualCryptLight.VisualCryptLib.Api.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Obsidian.Features.X1Wallet.Tests
{
    public class SigningTests
    {
        readonly ITestOutputHelper output;
        readonly FakeFactory factory;
        readonly Money fee;
        readonly string passphrase = "passwordpassword";
        readonly string walletName = nameof(SigningTests);
        readonly uint date = (uint) DateTime.UtcNow.Date.ToUnixTime();

        public SigningTests(ITestOutputHelper testOutputHelper)
        {
            this.output = testOutputHelper;
            this.factory = new FakeFactory(testOutputHelper);
            this.fee = Money.Coins(0.00023500m * 2);
        }


        [Fact]
        public void SignCoinstakeTransactions()
        {
            // Arrange

            // Delete previous wallet and create a new one, mine blocks.
            File.Delete(this.walletName.GetX1WalletFilepath(this.factory.Network, this.factory.DataFolder));
            this.factory.WalletManagerFactory.CreateWallet(new WalletCreateRequest { Passphrase = this.passphrase, WalletName = this.walletName });

            int blockMined = 200;
            long totalMiningReward = 0;

            for (var i = 0; i < blockMined; i++)
            {
                var transactions = new List<Transaction>();

                var zeroOneTwo = i % 3;
                var addressType = zeroOneTwo == 0 ? AddressType.PubKeyHash :
                    zeroOneTwo == 1 ? AddressType.ColdStakingHot : AddressType.MultiSig;

                transactions.Add(Create_Wallet.CreateCoinbase(this.factory.ChainIndexer.Tip.Height, this.factory.GetScriptPubKeyForMining(walletName, addressType), this.factory, out var reward));
                totalMiningReward += reward;
                ChainedHeaderBlock nextBlock = this.factory.AddNextBlock(transactions);
            }
            this.factory.SetIBD(false);
            this.factory.SyncToHeight(blockMined, this.walletName);

            var wm = this.factory.WalletManagerFactory.AutoLoad2(this.walletName);

            // Act

            // *** Sign PubKeyHash output
            var budget = wm.GetBalance(null, AddressType.PubKeyHash);
            var kernelCoin = budget.StakingCoins.First().Value;

            var txPubKeyHash1 = CoinstakeTransactionService.CreateCoinstakeTransaction(kernelCoin, Money.Coins(50).Satoshi, this.date, this.passphrase, out var privateKey1);
            var txPubKeyHash2 = txPubKeyHash1.Clone();
            Assert2.TransactionsEqual(txPubKeyHash1, txPubKeyHash2);

            // sign with TransactionBuilder
            txPubKeyHash1.Sign(C.Network, new[] { privateKey1 }, new ICoin[] { kernelCoin.ToCoin() });
            // sign with SigningService
            SigningService.SignInputs(txPubKeyHash2, new[] { privateKey1 }, new[] { kernelCoin });
            // same valid result?
            Assert2.IsSegWit(txPubKeyHash1);
            Assert2.TransactionsEqual(txPubKeyHash1, txPubKeyHash2);
          

            // *** Sign ColdStakingHot output
            var coldStakeKernelCoin = wm.GetBalance(null, AddressType.ColdStakingHot).StakingCoins.First().Value;
            var txColdStaking1 = CoinstakeTransactionService.CreateCoinstakeTransaction(coldStakeKernelCoin, Money.Coins(50).Satoshi, this.date, this.passphrase, out var privateKey2);
            var txColdStaking2 = txColdStaking1.Clone();
            Assert2.TransactionsEqual(txColdStaking1, txColdStaking2);

            // sign with TransactionBuilder
            SignScriptAddress(txColdStaking1, privateKey2, coldStakeKernelCoin);
            Assert2.IsSegWit(txColdStaking1);
            // sign with SigningService
            SigningService.SignInputs(txColdStaking2, new[] { privateKey2 }, new []{ coldStakeKernelCoin });
            // same valid result?
            Assert2.TransactionsEqual(txColdStaking1, txColdStaking2);

            // *** Sign MultiSig output
            var multiSigKernelCoin = wm.GetBalance(null, AddressType.MultiSig).StakingCoins.First().Value;

            var txMulti1 = CoinstakeTransactionService.CreateCoinstakeTransaction(multiSigKernelCoin, Money.Coins(50).Satoshi, this.date, this.passphrase, out var privateKey3);
            var txMulti2 = txMulti1.Clone();
            // sign with TransactionBuilder
            SignScriptAddress(txMulti1, privateKey3, multiSigKernelCoin);
            Assert2.IsSegWit(txMulti1);
            // sign with SigningService
            SigningService.SignInputs(txMulti2, new[] { privateKey3 }, new[] { multiSigKernelCoin });
            // same valid result?
            Assert2.TransactionsEqual(txMulti1, txMulti2);

        }

        public static void SignScriptAddress(Transaction tx, Key privateKey, SegWitCoin kernelCoin)
        {
            if (kernelCoin.SegWitAddress is MultiSigAddress multiSigAddress)
            {
                var sc = kernelCoin.ToCoin().ToScriptCoin(multiSigAddress.GetRedeemScript());
                tx.Sign(C.Network, new[] { privateKey }, new[] { sc });
            }
            else if (kernelCoin.SegWitAddress is ColdStakingAddress coldStakingAddress)
            {
                var sc = kernelCoin.ToCoin().ToScriptCoin(coldStakingAddress.GetRedeemScript());
                tx.Sign(C.Network, new[] { privateKey }, new[] { sc }, new[] { new ColdStakingBuilderExtension(staking: true) });
            }
        }
    }
}
