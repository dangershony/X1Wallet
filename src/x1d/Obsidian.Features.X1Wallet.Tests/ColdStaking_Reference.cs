using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using Obsidian.Networks.ObsidianX;
using Stratis.Bitcoin.Features.ColdStaking;
using Xunit;
using Xunit.Abstractions;

namespace Obsidian.Features.X1Wallet.Tests
{
    public class ColdStaking_Reference
    {
        readonly ITestOutputHelper output;
        readonly Money fee;

        public ColdStaking_Reference(ITestOutputHelper output)
        {
            this.output = output;
            this.fee = Money.Coins(0.00023500m *2);
        }

        [Fact]
        public void Original_CS_Setup_Tx()
        {
            var network = ObsidianXNetworksSelector.Obsidian.Mainnet();

            // I have received 100_000 in my wallet in this address
            Key myBudgetKey = new Key();
            PubKey myBudgetPubKey = myBudgetKey.PubKey.Compress();
            Transaction received = network.CreateTransaction();
            received.Outputs.Add(new TxOut(Money.Coins(100_000), myBudgetPubKey.WitHash.ScriptPubKey));
            List<Coin> myBudgetCoins = received.Outputs.AsCoins().ToList();

            // for 90_000, I want to set up ColdStaking
            Key coldKey = new Key();
            PubKey coldPubKey = coldKey.PubKey.Compress();
            Key hotKey = new Key();
            PubKey hotPubKey = hotKey.PubKey.Compress();

            Script csScriptPubKey = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKey.WitHash.AsKeyId(), coldPubKey.WitHash.AsKeyId());
            this.output.WriteLine(csScriptPubKey.ToString());

            TransactionBuilder builder = new TransactionBuilder(network);
            builder.Extensions.Add(new ColdStakingBuilderExtension(false));

            Transaction csSetupTx =
                builder
                    .AddCoins(myBudgetCoins)
                    .Send(csScriptPubKey, Money.Coins(90_000)) // 90_000 to cold staking
                    .SetChange(myBudgetPubKey.WitHash.ScriptPubKey) // 10_000 back to original source
                    .SendFees(this.fee)
                    .AddKeys(myBudgetKey)
                    .BuildTransaction(sign: true);

            

            bool isVerifyPassing = builder.Verify(csSetupTx, out var errors);
            foreach (var err in errors)
                this.output.WriteLine(err.ToString());

            this.output.WriteLine($"isVerifyPassing: {isVerifyPassing}");
            this.output.WriteLine($"hasWitness: {csSetupTx.HasWitness}");
            this.output.WriteLine(csSetupTx.ToString());

            Assert.False(isVerifyPassing);
            Assert.True(csSetupTx.HasWitness);
        }

       
        [Fact]
        public void SegWit_CS_Setup_Tx_and_Withdrawal()
        {
            var network = ObsidianXNetworksSelector.Obsidian.Mainnet();

            // I have received 100_000 in my wallet in this address
            Key myBudgetKey = new Key();
            PubKey myBudgetPubKey = myBudgetKey.PubKey.Compress();
            Transaction received = network.CreateTransaction();
            received.Outputs.Add(new TxOut(Money.Coins(100_000), myBudgetPubKey.WitHash.ScriptPubKey));
            List<Coin> myBudgetCoins = received.Outputs.AsCoins().ToList();

            // for 90_000, I want to set up ColdStaking
            Key coldKey = new Key();
            PubKey coldPubKey = coldKey.PubKey.Compress();
            Key hotKey = new Key();
            PubKey hotPubKey = hotKey.PubKey.Compress();

            Script csRedeemScript = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKey.WitHash.AsKeyId(), coldPubKey.WitHash.AsKeyId());
            this.output.WriteLine($"csRedeemScript: {csRedeemScript}");

            string csScriptAddress = csRedeemScript.WitHash.GetAddress(network).ToString();
            this.output.WriteLine($"{nameof(csScriptAddress)}: {csScriptAddress}");

            Script csScriptAddressScriptPubKey = csRedeemScript.WitHash.ScriptPubKey;
            this.output.WriteLine($"{nameof(csScriptAddressScriptPubKey)}: {csScriptAddressScriptPubKey}");


            var builder = new TransactionBuilder(network);

            Transaction csSetupTx =
                builder
                    .AddCoins(myBudgetCoins)
                    .Send(csScriptAddressScriptPubKey, Money.Coins(90_000)) // 90_000 to cold staking script address
                    .SetChange(myBudgetPubKey.WitHash.ScriptPubKey) // 10_000 back to original source
                    .SendFees(this.fee)
                    .AddKeys(myBudgetKey)
                    .BuildTransaction(sign: true);

            bool isVerifyPassing = builder.Verify(csSetupTx, out var errors);
            foreach (var err in errors)
                this.output.WriteLine(err.ToString());

            bool hasEmptyScriptSig = csSetupTx.Inputs.All(i => i.ScriptSig.Length == 0);

            this.output.WriteLine($"isVerifyPassing: {isVerifyPassing}");
            this.output.WriteLine($"hasWitness: {csSetupTx.HasWitness}");
            this.output.WriteLine($"hasEmptyScriptSig: {hasEmptyScriptSig}");
            this.output.WriteLine(csSetupTx.ToString());

            Assert.True(isVerifyPassing);
            Assert.True(csSetupTx.HasWitness);
            Assert.True(hasEmptyScriptSig);

            this.output.WriteLine("*** Now, immediate Withdrawal after the CS Setup tx ***");

            var csScriptCoins = csSetupTx.Outputs.AsCoins().Select(cs=>cs.ToScriptCoin(csRedeemScript)).ToList();
            var builderWithdrawal = new TransactionBuilder(network);
            builderWithdrawal.Extensions.Add(new ColdStakingBuilderExtension(false));


            Transaction withDrawTx =
            builderWithdrawal
                .AddCoins(csScriptCoins)
                .Send(myBudgetPubKey.WitHash.ScriptPubKey, Money.Coins(80_000)) // withdraw 80_000 of the 90_000 I previously sent to the cold staking script address
                .SetChange(csScriptAddressScriptPubKey) // Change goes back the CS script address
                .SendFees(this.fee)
                .AddKeys(coldKey) // use the cold private key for the withdrawal
                .BuildTransaction(sign: true);


            bool isWithdrawVerifyPassing = builderWithdrawal.Verify(withDrawTx, out var errors2);
            foreach (var err in errors2)
                this.output.WriteLine(err.ToString());

            bool hasEmptyScriptSig2 = withDrawTx.Inputs.All(i => i.ScriptSig.Length == 0);

            this.output.WriteLine($"isVerifyPassing: {isWithdrawVerifyPassing}");
            this.output.WriteLine($"hasWitness: {withDrawTx.HasWitness}");
            this.output.WriteLine($"hasEmptyScriptSig: {hasEmptyScriptSig2}");
            this.output.WriteLine(withDrawTx.ToString());

            Assert.True(isWithdrawVerifyPassing);
            Assert.True(withDrawTx.HasWitness);
            Assert.True(hasEmptyScriptSig2);

        }
    }
}
