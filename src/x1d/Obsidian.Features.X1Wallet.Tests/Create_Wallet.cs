using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Tools;
using Obsidian.Networks.ObsidianX.Rules;
using Stratis.Bitcoin.EventBus.CoreEvents;
using Stratis.Bitcoin.Primitives;

namespace Obsidian.Features.X1Wallet.Tests
{
    using Microsoft.Extensions.Logging;
    using NBitcoin;
    using Obsidian.Features.X1Wallet.Tests.Fakes;
    using Obsidian.Networks.ObsidianX;
    using Xunit;
    using Xunit.Abstractions;

    public class Create_Wallet
    {
        readonly ITestOutputHelper output;
        readonly FakeFactory factory;

        const string passphrase = "passwordpassword";
        const string walletName = "testWallet";

        public Create_Wallet(ITestOutputHelper testOutputHelper)
        {
            this.output = testOutputHelper;
            this.factory = new FakeFactory(testOutputHelper);
        }

        [Fact]
        public void CreateWallet()
        {
            // delete previous wallet
            File.Delete(walletName.GetX1WalletFilepath(this.factory.Network, this.factory.DataFolder));

            // create new wallet
            this.factory.WalletManagerFactory.CreateWallet(new WalletCreateRequest { Passphrase = passphrase, WalletName = walletName });

            // now, create a new WalletManagerFactory instance.
            this.factory.DisposeAndRecreateWalletManagerFactory();

            // load the created wallet from file, and check if the addresses have been created,
            using (var context = this.factory.WalletManagerFactory.AutoLoad(walletName))
            {
                var addresses = context.WalletManager.GetPubKeyHashAddresses(C.External, null);
                var changeAddresses = context.WalletManager.GetPubKeyHashAddresses(C.Change, null);
                Assert.Equal(C.GapLimit, addresses.Length);
                Assert.Equal(C.GapLimit, changeAddresses.Length);

                // the balance of a new wallet must be zero
                var balance = context.WalletManager.GetBalance();

                Assert.True(balance.Total == 0 && balance.Confirmed == 0 && balance.Pending == 0 && balance.Spendable == 0 &&
                            balance.Stakable == 0);
                Assert.True(balance.SpendableCoins.Count == 0 && balance.StakingCoins.Count == 0);
            }

            int blockMined = 200;
            long totalMiningReward = 0;

            for (var i = 0; i < blockMined; i++)
            {
                var transactions = new List<Transaction>();
                transactions.Add(CreateCoinbase(this.factory.ChainIndexer.Tip.Height, this.factory.GetScriptPubKeyForMining(walletName), out var reward));
                totalMiningReward += reward;
                ChainedHeaderBlock nextBlock = this.factory.AddNextBlock(transactions);
            }

            this.factory.SetIBD(false);

            this.factory.SyncToHeight(blockMined, walletName);


            using (var context = this.factory.WalletManagerFactory.AutoLoad(walletName))
            {

                var balance = context.WalletManager.GetBalance();
                Assert.True(balance.Confirmed == totalMiningReward && balance.Pending == 0);

                long spendableBlocks = blockMined - this.factory.Network.Consensus.CoinbaseMaturity + 1;
                Assert.True(balance.SpendableCoins.Count == spendableBlocks);

                long stakableBlocks = blockMined - this.factory.Network.Consensus.MaxReorgLength + 1;
                Assert.True(balance.StakingCoins.Count == stakableBlocks);

                //Assert.True(balance.Spendable == 0 && balance.Stakable == 0);
                //Assert.True(coins.Length == 0);
            }

        }



        /// <summary>
        /// Create coinbase transaction.
        /// Set the coin base with zero money.
        /// Once we have the fee we can update the amount.
        /// </summary>
        public Transaction CreateCoinbase(int currentHeight, Script scriptPubKey, out long reward)
        {
            var coinbase = this.factory.Network.CreateTransaction();

            reward = currentHeight == this.factory.Network.Consensus.PremineHeight
                ? this.factory.Network.Consensus.PremineReward
                : this.factory.Network.Consensus.ProofOfWorkReward;
            coinbase.AddInput(TxIn.CreateCoinbase(currentHeight + 1));
            coinbase.AddOutput(new TxOut(reward, scriptPubKey));
            return coinbase;
        }


        [Fact]
        public void WalletController_cant_load_wallet_without_walletName()
        {
            var controller = this.factory.CreateWalletController();

            Assert.NotNull(controller);

            var ex = Assert.Throws<ArgumentNullException>(() => controller.LoadWallet());
            Assert.Equal("walletName", ex.ParamName);
        }

        [Fact]
        public void DateConversion()
        {
            var dateTime = DateTime.UtcNow.Date.AddSeconds(23);
            long nowUnixSeconds = dateTime.ToUnixTime();
            Assert.Equal(dateTime, nowUnixSeconds.ToDateTimeUtc());
            Assert.Equal(DateTimeKind.Utc, nowUnixSeconds.ToDateTimeUtc().Kind);

            var tooFarIntoFuture = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToUnixTime();

            Assert.Throws<ArgumentOutOfRangeException>(() => tooFarIntoFuture.ToDateTimeUtc());
        }
    }
}
