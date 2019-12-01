using System;
using System.Linq;
using NBitcoin;
using Stratis.Bitcoin.Features.ColdStaking;
using VisualCrypt.VisualCryptLight.VisualCryptLib.Api.Infrastructure;
using Xunit;

namespace Obsidian.Features.X1Wallet.Tests
{
    static class Assert2
    {
        public static void TransactionsEqual(Transaction expected, Transaction actual)
        {
            Assert.True(ByteArrays.AreAllBytesEqual(expected.ToBytes(), actual.ToBytes()), "Expected the transactions where equal byte-for-byte, but they were not");
            Assert.True(expected.GetHash() == actual.GetHash(),
                "Expected both transactions to have the same hash, but they had not.");
        }

        public static void IsSegWit(Transaction transaction)
        {
            bool hasEmptyScriptSig = transaction.Inputs.All(i => i.ScriptSig.Length == 0);
            Assert.True(hasEmptyScriptSig);
            Assert.True(transaction.HasWitness);
        }
    }
}
