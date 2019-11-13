using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Tests.Fakes;
using Obsidian.Networks.ObsidianX;
using Xunit;
using Xunit.Abstractions;

namespace Obsidian.Features.X1Wallet.Tests
{
    public class Create_Wallet
    {
        readonly ITestOutputHelper output;



        public Create_Wallet(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Create_wallet()
        {
            var controller = FakeFactory.Instance.Value.CreateWalletController();
            Assert.NotNull(controller);
            var response = controller.LoadWallet();
           
        }

       

       
    }
}
