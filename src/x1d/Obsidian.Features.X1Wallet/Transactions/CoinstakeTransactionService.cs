using System;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Features.ColdStaking;
using VisualCrypt.VisualCryptLight.VisualCryptLib.Api.Infrastructure;

namespace Obsidian.Features.X1Wallet.Transactions
{
    sealed class CoinstakeTransactionService
    {
        internal Transaction CreateCoinstakeTransaction(SegWitCoin kernelCoin, long totalReward, uint currentBlockTime, string passphrase, out Key privateKey)
        {

            var tx = CreateTransaction(kernelCoin, totalReward, currentBlockTime, passphrase, out privateKey);

            if (kernelCoin.SegWitAddress.AddressType != Models.Wallet.AddressType.PubKeyHash)
            {
                SignScriptAddress(tx, privateKey, kernelCoin);
                return tx;
            }


            tx.Sign(C.Network, new[] { privateKey }, new ICoin[] { kernelCoin.ToCoin() });

            var txBytes = tx.ToBytes();
            var txHash = tx.GetHash();

            var tx2 = CreateTransaction(kernelCoin, totalReward, currentBlockTime, passphrase, out privateKey);
            SigningService.SignInputs(tx2, new[] { privateKey }, new[] { kernelCoin });

            var tx2Bytes = tx.ToBytes();
            var tx2Hash = tx.GetHash();

            if (!ByteArrays.AreAllBytesEqual(txBytes, tx2Bytes))
            {
                throw new InvalidOperationException($"tx.Sign and SigningService create different results for {kernelCoin.SegWitAddress.AddressType} - byte arrays not equal.");
            }
            if (txHash != tx2Hash)
            {
                throw new InvalidOperationException($"tx.Sign and SigningService create different results for {kernelCoin.SegWitAddress.AddressType} - hash not equal.");
            }



            return tx;
        }

        void SignScriptAddress(Transaction tx, Key privateKey, SegWitCoin kernelCoin)
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

        Transaction CreateTransaction(SegWitCoin kernelCoin, long totalReward, uint currentBlockTime, string passphrase, out Key privateKey)
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
