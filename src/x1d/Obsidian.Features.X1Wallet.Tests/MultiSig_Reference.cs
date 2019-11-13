using System;
using System.Linq;
using NBitcoin;
using Obsidian.Networks.ObsidianX;
using Xunit;
using Xunit.Abstractions;

namespace Obsidian.Features.X1Wallet.Tests
{
    public class MultiSig_Reference
    {
        readonly ITestOutputHelper output;

        public MultiSig_Reference(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Native_MultiSig_two_of_three_no_scripthash_no_witness()
        {
            var network = ObsidianXNetworksSelector.Obsidian.Mainnet();
            Key bob = new Key();
            Key alice = new Key();
            Key satoshi = new Key();

            Script scriptPubKey = PayToMultiSigTemplate
                .Instance
                .GenerateScriptPubKey(2, new[] { bob.PubKey.Compress(), alice.PubKey.Compress(), satoshi.PubKey.Compress() });

            this.output.WriteLine(scriptPubKey.ToString());

            Transaction received = network.CreateTransaction();
            received.Outputs.Add(new TxOut(Money.Coins(5), scriptPubKey));

            Coin coin = received.Outputs.AsCoins().First();

            BitcoinWitPubKeyAddress spendMultiSigToAddress = new Key().PubKey.Compress().GetSegwitAddress(network);
            TransactionBuilder builder = new TransactionBuilder(network);
           
            Transaction unsigned =
                builder
                    .AddCoins(coin)
                    .Send(spendMultiSigToAddress, Money.Coins(1.0m))
                    .SetChange(scriptPubKey)
                    .SendFees(Money.Satoshis(100))
                    .BuildTransaction(sign: false);

            Transaction aliceSigned =
                builder
                    .AddCoins(coin)
                    .AddKeys(alice)
                    .SignTransaction(unsigned);

            Transaction bobSigned =
                builder
                    .AddCoins(coin)
                    .AddKeys(bob)
                    //At this line, SignTransaction(unSigned) has the identical functionality with the SignTransaction(aliceSigned).
                    //It's because unsigned transaction has already been signed by Alice privateKey from above.
                    .SignTransaction(aliceSigned);

            Transaction fullySigned =
                builder
                    .AddCoins(coin)
                    .CombineSignatures(aliceSigned, bobSigned);

            bool isVerifyPassing = builder.Verify(fullySigned, out var errors);
            foreach (var err in errors)
                this.output.WriteLine(err.ToString());

            this.output.WriteLine($"isVerifyPassing: {isVerifyPassing}");
            this.output.WriteLine(fullySigned.ToString());

            Assert.True(isVerifyPassing);
            Assert.False(fullySigned.HasWitness);
        }

        [Fact]
        public void P2WSH_MultiSig_two_of_three()
        {
            var network = ObsidianXNetworksSelector.Obsidian.Mainnet();
            Key bob = new Key();
            Key alice = new Key();
            Key satoshi = new Key();

            Script msRedeemScript = PayToMultiSigTemplate
                .Instance
                .GenerateScriptPubKey(2, new[] { bob.PubKey.Compress(), alice.PubKey.Compress(), satoshi.PubKey.Compress() });

            this.output.WriteLine($"{nameof(msRedeemScript)}: {msRedeemScript}");
            this.output.WriteLine($"{nameof(msRedeemScript)}.{nameof(msRedeemScript.PaymentScript)}: {msRedeemScript.PaymentScript}");
            this.output.WriteLine("In P2SH payments, we refer to the hash of the Redeem Script as the ScriptPubKey:");
            this.output.WriteLine($"{nameof(msRedeemScript)}.{nameof(msRedeemScript.WitHash)}: {msRedeemScript.WitHash}");
            this.output.WriteLine($"{nameof(msRedeemScript)}.{nameof(msRedeemScript.WitHash)}.{nameof(msRedeemScript.WitHash.ScriptPubKey)}: {msRedeemScript.WitHash.ScriptPubKey}");

            string bech32ScriptAddress = msRedeemScript.WitHash.GetAddress(network).ToString();
            Assert.Equal(new BitcoinWitScriptAddress(bech32ScriptAddress,network).ToString(), bech32ScriptAddress);


            this.output.WriteLine($"{nameof(msRedeemScript)}.{nameof(msRedeemScript.WitHash)}.GetAddress(network): {msRedeemScript.WitHash.GetAddress(network)}");

            Script msScriptPubKey = msRedeemScript.WitHash.ScriptPubKey;  // In P2SH payments, we refer to the hash of the Redeem Script as the ScriptPubKey.

            // Receive 5 coins on the ms address we just created
            Transaction received = network.CreateTransaction();
            received.Outputs.Add(new TxOut(Money.Coins(5), msRedeemScript.WitHash.ScriptPubKey));
           
            // Spend 1 coin from the 5 coins we received
            Coin coin = received.Outputs.AsCoins().First().ToScriptCoin(msRedeemScript);

            BitcoinWitPubKeyAddress spendMultiSigToAddress = new Key().PubKey.Compress().GetSegwitAddress(network);
            TransactionBuilder builder = new TransactionBuilder(network);

            Transaction unsigned =
                builder
                    .AddCoins(coin)
                    .Send(spendMultiSigToAddress, Money.Coins(1.0m))
                    .SetChange(msRedeemScript.WitHash.ScriptPubKey)
                    .SendFees(Money.Satoshis(100))
                    .BuildTransaction(sign: false);

            Transaction aliceSigned =
                builder
                    .AddCoins(coin)
                    .AddKeys(alice)
                    .SignTransaction(unsigned);

            Transaction bobSigned =
                builder
                    .AddCoins(coin)
                    .AddKeys(bob)
                    //At this line, SignTransaction(unSigned) has the identical functionality with the SignTransaction(aliceSigned).
                    //It's because unsigned transaction has already been signed by Alice privateKey from above.
                    .SignTransaction(aliceSigned);

            Transaction fullySigned =
                builder
                    .AddCoins(coin)
                    .CombineSignatures(aliceSigned, bobSigned);

            bool isVerifyPassing = builder.Verify(fullySigned, out var errors);
            foreach (var err in errors)
                this.output.WriteLine(err.ToString());

            this.output.WriteLine($"isVerifyPassing: {isVerifyPassing}");
            this.output.WriteLine(fullySigned.ToString());

            Assert.True(isVerifyPassing);
            Assert.True(fullySigned.HasWitness);
        }

        [Fact]
        public void P2WSH_MultiSig_one_of_two()
        {
            var network = ObsidianXNetworksSelector.Obsidian.Mainnet();

            Key myPrivateKey = new Key();
            PubKey alicePublicKey = new Key().PubKey.Compress();

            Script msRedeemScript = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(1, myPrivateKey.PubKey.Compress(), alicePublicKey);

            this.output.WriteLine($"{nameof(msRedeemScript)}: {msRedeemScript}");
            this.output.WriteLine($"{nameof(msRedeemScript)}.{nameof(msRedeemScript.PaymentScript)}: {msRedeemScript.PaymentScript}");
            this.output.WriteLine("In P2SH payments, we refer to the hash of the Redeem Script as the ScriptPubKey:");
            this.output.WriteLine($"{nameof(msRedeemScript)}.{nameof(msRedeemScript.WitHash)}: {msRedeemScript.WitHash}");
            this.output.WriteLine($"{nameof(msRedeemScript)}.{nameof(msRedeemScript.WitHash)}.{nameof(msRedeemScript.WitHash.ScriptPubKey)}: {msRedeemScript.WitHash.ScriptPubKey}");

            string bech32ScriptAddress = msRedeemScript.WitHash.GetAddress(network).ToString();
            this.output.WriteLine($"{nameof(msRedeemScript)}.{nameof(msRedeemScript.WitHash)}.GetAddress(network): {bech32ScriptAddress}");

            Script msScriptPubKey = msRedeemScript.WitHash.ScriptPubKey;  // In P2SH payments, we refer to the hash of the Redeem Script as the ScriptPubKey.

            // Receive 5 coins on the ms address we just created
            Transaction received = network.CreateTransaction();
            received.Outputs.Add(new TxOut(Money.Coins(5), msRedeemScript.WitHash.ScriptPubKey));

            // Spend 1 coin from the 5 coins we received
            Coin coin = received.Outputs.AsCoins().First().ToScriptCoin(msRedeemScript);

            BitcoinWitPubKeyAddress spendMultiSigToAddress = new Key().PubKey.Compress().GetSegwitAddress(network);

            TransactionBuilder builder = new TransactionBuilder(network);

            Transaction unsigned =
                builder
                    .AddCoins(coin)
                    .Send(spendMultiSigToAddress, Money.Coins(1.0m))
                    .SetChange(msRedeemScript.WitHash.ScriptPubKey)
                    .SendFees(Money.Satoshis(100))
                    .BuildTransaction(sign: false);

            Transaction mySigned = builder
                    .AddCoins(coin)
                    .AddKeys(myPrivateKey)
                    .SignTransaction(unsigned);

           

            Transaction fullySigned =
                builder
                    .AddCoins(coin)
                    .CombineSignatures(mySigned);

            bool isVerifyPassing = builder.Verify(fullySigned, out var errors);
            foreach (var err in errors)
                this.output.WriteLine(err.ToString());

            this.output.WriteLine($"isVerifyPassing: {isVerifyPassing}");
            this.output.WriteLine(fullySigned.ToString());

            Assert.True(isVerifyPassing);
            Assert.True(fullySigned.HasWitness);
        }
    }
}
