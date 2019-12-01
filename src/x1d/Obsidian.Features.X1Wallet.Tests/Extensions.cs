using NBitcoin;
using Obsidian.Features.X1Wallet.Tools;
namespace Obsidian.Features.X1Wallet.Tests
{
    public static class Extensions
    {
        public static Transaction Clone(this Transaction transaction)
        {
            var clone = new Transaction();
            clone.ReadWrite(transaction.ToBytes(), C.Network.Consensus.ConsensusFactory);
            return clone;
        }
    }
}
