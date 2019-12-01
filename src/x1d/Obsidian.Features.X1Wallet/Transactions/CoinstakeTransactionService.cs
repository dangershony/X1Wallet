using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;

namespace Obsidian.Features.X1Wallet.Transactions
{
    static class CoinstakeTransactionService
    {
        public static Transaction CreateAndSignCoinstakeTransaction(SegWitCoin kernelCoin, long totalReward, uint currentBlockTime, string passphrase, out Key privateKey)
        {
            var tx = CreateCoinstakeTransaction(kernelCoin, totalReward, currentBlockTime, passphrase, out privateKey);

            SigningService.SignInputs(tx, new[] {privateKey}, new[] {kernelCoin});
           
            return tx;
        }

        public static Transaction CreateCoinstakeTransaction(SegWitCoin kernelCoin, long totalReward, uint currentBlockTime, string passphrase, out Key privateKey)
        {
            Transaction tx = C.Network.CreateTransaction();

            if (tx is PosTransaction posTransaction)
                posTransaction.Time = currentBlockTime;

            if (kernelCoin.SegWitAddress is ColdStakingAddress coldStakingAddress &&
                coldStakingAddress.AddressType == AddressType.ColdStakingHot)
                privateKey = new Key(coldStakingAddress.StakingKey);
            else
                privateKey = kernelCoin.GetPrivateKey(passphrase);

            tx.AddInput(new TxIn(new OutPoint(kernelCoin.UtxoTxHash, kernelCoin.UtxoTxN)));

            tx.Outputs.Add(new TxOut(0, Script.Empty));
            tx.Outputs.Add(new TxOut(0, new Script(OpcodeType.OP_RETURN, Op.GetPushOp(privateKey.PubKey.Compress().ToBytes()))));
            tx.Outputs.Add(new TxOut(totalReward + kernelCoin.UtxoValue, kernelCoin.SegWitAddress.GetScriptPubKey()));
            return tx;
        }
    }
}
