using System;
using System.IO;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Tools;

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
            var walletManagerFactory = this.factory.CreateWalletManagerFactory();

            var path = walletName.GetX1WalletFilepath(this.factory.network, this.factory.dataFolder);
            File.Delete(path);
            walletManagerFactory.CreateWallet(new Models.Api.Requests.WalletCreateRequest
            { Passphrase = passphrase, WalletName = walletName });

            walletManagerFactory.Dispose();

            // now, create a new WalletManagerFactory instance.
            walletManagerFactory = this.factory.CreateWalletManagerFactory();
          
           // load the created wallet from file, and check if the addresses have been created,
            using (var context = walletManagerFactory.AutoLoad(walletName))
            {
                var addresses = context.WalletManager.GetPubKeyHashAddresses(C.External, null);
                var changeAddresses = context.WalletManager.GetPubKeyHashAddresses(C.Change, null);
                Assert.Equal(C.GapLimit, addresses.Length);
                Assert.Equal(C.GapLimit, changeAddresses.Length);
            }
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
