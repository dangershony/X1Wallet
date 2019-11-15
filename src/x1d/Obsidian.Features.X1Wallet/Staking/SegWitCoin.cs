using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Wallet;

namespace Obsidian.Features.X1Wallet.Staking
{
    public sealed class SegWitCoin
    {
        public readonly ISegWitAddress SegWitAddress;

        public readonly uint256 UtxoTxHash;
        public readonly int UtxoTxN;
        public readonly long UtxoValue;

        public readonly uint? UtxoPosV3Time;

        public SegWitCoin(ISegWitAddress segWitAddress, uint256 utxoTxHash, int utxoTxN, long utxoValue)
        {
            this.SegWitAddress = segWitAddress;
            this.UtxoTxHash = utxoTxHash;
            this.UtxoTxN = utxoTxN;
            this.UtxoValue = utxoValue;
        }

        public SegWitCoin(ISegWitAddress segWitAddress, uint256 utxoTxHash, int utxoTxN, long utxoValue, uint? utxoPosV3Time) : this(segWitAddress, utxoTxHash, utxoTxN, utxoValue)
        {
            this.UtxoPosV3Time = utxoPosV3Time;
        }
    }
}
