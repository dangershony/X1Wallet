using System;
using System.Diagnostics;
using System.Linq;
using NBitcoin;
using NBitcoin.Crypto;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;

namespace Obsidian.Features.X1Wallet.Transactions
{
    public class SigningService
    {
        public static void SignInputs(Transaction transaction, Key[] keys, StakingCoin[] coins)
        {
            for (var i = 0; i < transaction.Inputs.Count; i++)
            {
                var txin = transaction.Inputs[i];
                var key = keys[i];
                var coin = coins[i];
                SignInput(txin, key, coin, i, transaction);
            }
        }

        static void SignInput(TxIn txin, Key key, StakingCoin coin, int index, Transaction transaction)
        {
            if (coin.RedeemScript == null)
            {
                uint256 signatureHash = GetHashToSign(transaction, index, coin.Address, coin.TxOut.Value);

                byte[] finalSig = GetSignature(signatureHash, key);

                txin.WitScript = new WitScript(Op.GetPushOp(finalSig),
                    Op.GetPushOp(key.PubKey.Compress().ToBytes()));
            }
            else
            {
                uint256 signatureHash = GetHashToSignMultiSig(transaction, index, coin, coin.TxOut.Value);

                byte[] finalSig = GetSignature(signatureHash, key);

                //txin.WitScript = new WitScript(Op.GetPushOp(finalSig),
                //    Op.GetPushOp(key.PubKey.Compress().ToBytes()));

                //txin.WitScript = new WitScript(Op.GetPushOp(finalSig), // error Stack
                //   Op.GetPushOp(coin.RedeemScript.ToBytes()));

                // ([sig ...] num_of_signatures [pubkey ...] num_of_pubkeys -- bool)

                var result = new Script(OpcodeType.OP_0); // pop-one-too-many workaround

                result += Op.GetPushOp(finalSig);
                //result += OpcodeType.OP_0;
                //result += Op.GetPushOp(key.PubKey.Compress().ToBytes());
                // result += Op.GetPushOp(coin.RedeemScript.ToBytes());
                //https://www.soroushjp.com/2014/12/20/bitcoin-multisig-the-hard-way-understanding-raw-multisignature-bitcoin-transactions/
                System.Collections.Generic.IList<Op> ops = result.ToOps();
                foreach (var o in coin.RedeemScript.ToOps())
                    ops.Add(o);
                var w = new WitScript(ops.ToArray());
                txin.WitScript = w;
                //txin.WitScript = new WitScript(
                //    Op.GetPushOp(finalSig),
                //    Op.GetPushOp(1),
                //    Op.GetPushOp(key.PubKey.Compress().ToBytes()),
                //    //Op.GetPushOp(1),
                //    Op.GetPushOp(coin.RedeemScript.ToBytes())

                //   );
            }

        }

        static byte[] GetSignature(uint256 hashToSign, Key key)
        {
            var signature = key.Sign(hashToSign, SigHash.All);
            ECDSASignature ecdsaSig = signature.Signature;
            byte[] derSig = ecdsaSig.ToDER();
            byte[] finalSig = new byte[derSig.Length + 1];
            Array.Copy(derSig, 0, finalSig, 0, derSig.Length);
            finalSig[finalSig.Length - 1] = (byte)SigHash.All;
            return finalSig;
        }

        static uint256 GetHashToSign(Transaction tx, int index, string address, Money amount)
        {
            Script scriptCode = GetScriptCode(address.ScriptPubKeyFromBech32Safe());

            const SigHash nHashType = SigHash.All;

            if (amount == null)
                throw new ArgumentException("The amount of the output being signed must be provided", nameof(amount));

            uint256 hashPrevouts = GetHashPrevouts(tx);
            uint256 hashSequence = GetHashSequence(tx);
            uint256 hashOutputs = GetHashOutputs(tx);

            BitcoinStream sss = CreateHashWriter(HashVersion.Witness);
            // Version
            sss.ReadWrite(tx.Version);
            // Input prevouts/nSequence (none/all, depending on flags)
            sss.ReadWrite(hashPrevouts);
            sss.ReadWrite(hashSequence);
            // The input being signed (replacing the scriptSig with scriptCode + amount)
            // The prevout may already be contained in hashPrevout, and the nSequence
            // may already be contain in hashSequence.
            sss.ReadWrite(tx.Inputs[index].PrevOut);
            sss.ReadWrite(scriptCode);
            sss.ReadWrite(amount.Satoshi);
            sss.ReadWrite((uint)tx.Inputs[index].Sequence);
            // Outputs (none/one/all, depending on flags)
            sss.ReadWrite(hashOutputs);
            // Locktime
            sss.ReadWriteStruct(tx.LockTime);
            // Sighash type
            sss.ReadWrite((uint)nHashType);

            uint256 hashToSign = GetHash(sss);

            return hashToSign;
        }


        static uint256 GetHashToSignMultiSig(Transaction tx, int index, StakingCoin coin, Money amount)
        {
            bool isRedeemValid = PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(coin.ScriptPubKey);
            WitKeyId key = PayToWitPubKeyHashTemplate.Instance.ExtractScriptPubKeyParameters(null, coin.ScriptPubKey);
            Script scriptCode;
            if (key != null)
                scriptCode = key.AsKeyId().ScriptPubKey;
            else
                scriptCode = coin.ScriptPubKey;

            const SigHash nHashType = SigHash.All;

            if (amount == null)
                throw new ArgumentException("The amount of the output being signed must be provided", nameof(amount));

            uint256 hashPrevouts = GetHashPrevouts(tx);
            uint256 hashSequence = GetHashSequence(tx);
            uint256 hashOutputs = GetHashOutputs(tx);

            BitcoinStream sss = CreateHashWriter(HashVersion.Witness);
            // Version
            sss.ReadWrite(tx.Version);
            // Input prevouts/nSequence (none/all, depending on flags)
            sss.ReadWrite(hashPrevouts);
            sss.ReadWrite(hashSequence);
            // The input being signed (replacing the scriptSig with scriptCode + amount)
            // The prevout may already be contained in hashPrevout, and the nSequence
            // may already be contain in hashSequence.
            sss.ReadWrite(tx.Inputs[index].PrevOut);
            sss.ReadWrite(scriptCode);
            sss.ReadWrite(amount.Satoshi);
            sss.ReadWrite((uint)tx.Inputs[index].Sequence);
            // Outputs (none/one/all, depending on flags)
            sss.ReadWrite(hashOutputs);
            // Locktime
            sss.ReadWriteStruct(tx.LockTime);
            // Sighash type
            sss.ReadWrite((uint)nHashType);

            uint256 hashToSign = GetHash(sss);

            return hashToSign;
        }



        static Script GetScriptCode(Script scriptPubKey)
        {
            WitKeyId key = PayToWitPubKeyHashExtractScriptPubKeyParameters(scriptPubKey);
            KeyId keyId = key.AsKeyId();
            var scriptCode = keyId.ScriptPubKey;
            Debug.Assert(scriptPubKey != scriptCode);
            return scriptCode;
        }

        static WitKeyId PayToWitPubKeyHashExtractScriptPubKeyParameters(Script scriptPubKey)
        {
            var data = new byte[20];
            Array.Copy(scriptPubKey.ToBytes(true), 2, data, 0, 20);
            return new WitKeyId(data);
        }

        static uint256 GetHashPrevouts(Transaction txTo)
        {
            uint256 hashPrevouts;
            BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
            foreach (TxIn input in txTo.Inputs)
            {
                ss.ReadWrite(input.PrevOut);
            }
            hashPrevouts = GetHash(ss);
            return hashPrevouts;
        }

        static uint256 GetHashOutputs(Transaction txTo)
        {
            uint256 hashOutputs;
            BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
            foreach (TxOut txout in txTo.Outputs)
            {
                ss.ReadWrite(txout);
            }
            hashOutputs = GetHash(ss);
            return hashOutputs;
        }

        static uint256 GetHashSequence(Transaction txTo)
        {
            uint256 hashSequence;
            BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
            foreach (TxIn input in txTo.Inputs)
            {
                ss.ReadWrite((uint)input.Sequence);
            }
            hashSequence = GetHash(ss);
            return hashSequence;
        }

        static BitcoinStream CreateHashWriter(HashVersion version)
        {
            var hs = new HashStream();
            var stream = new BitcoinStream(hs, true);
            stream.Type = SerializationType.Hash;
            stream.TransactionOptions = version == HashVersion.Original ? TransactionOptions.None : TransactionOptions.Witness;
            return stream;
        }

        static uint256 GetHash(BitcoinStream stream)
        {
            uint256 preimage = ((HashStream)stream.Inner).GetHash();
            stream.Inner.Dispose();
            return preimage;
        }

    }
}
