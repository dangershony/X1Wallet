using System;
using System.Linq;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Features.ColdStaking;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Addresses
{
    static class MultiSigAddressService
    {
        internal static void EnsureDefaultMultisigAddress(string passphrase, X1WalletFile x1WalletFile)
        {
            //if (x1WalletFile.ColdStakingAddresses.Count > 0)
            //    return;

            var decryptedSeed = VCL.DecryptWithPassphrase(passphrase, x1WalletFile.HdSeed);

            KeyMaterial myKeyMaterial = KeyHelper.CreateHdKeyMaterial(decryptedSeed, passphrase, C.Network.Consensus.CoinType, AddressType.MultiSig, C.External, 0);
            PubKey myPubKey = myKeyMaterial.GetKey(passphrase).PubKey.Compress();

            // other Key
            KeyMaterial otherKeyMaterial = KeyHelper.CreateHdKeyMaterial(decryptedSeed, passphrase, C.Network.Consensus.CoinType, AddressType.MultiSig, C.External, 1);
            var otherPubKey = otherKeyMaterial.GetKey(passphrase).PubKey.Compress();

            // The redeem script looks like:
            // 1 03fad6426522dbda5c5a9f8cab24a54ccc374517ad8790bf7e5a14308afc1bf77b 0340ecf2e20978075a49369e35269ecf0651d2f48061ebbf918f3eb1964051f65c 2 OP_CHECKMULTISIG
            Script redeemScript = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(1, myPubKey, otherPubKey);

            // The address looks like:
            // odx1qvar8r29r8llzj53q5utmcewpju59263h38250ws33lp2q45lmalqg5lmdd
            string bech32ScriptAddress = redeemScript.WitHash.GetAddress(C.Network).ToString();

            // In P2SH payments, we refer to the hash of the Redeem Script as the scriptPubKey. It looks like:
            // 0 674671a8a33ffe295220a717bc65c19728556a3789d547ba118fc2a0569fdf7e
            Script scriptPubKey = redeemScript.WitHash.ScriptPubKey;
            var scp = scriptPubKey.ToString();


            var multiSigAddress = new MultiSigAddress
            {
                OwnKey = myKeyMaterial, Address = bech32ScriptAddress, AddressType = AddressType.MultiSig,
                Label = "Default 1-of-2 MultiSig", MaxSignatures = 2,
                LastSeenHeight = null,
                SignaturesRequired = 1,
                RedeemScriptHex = redeemScript.ToBytes().ToHexString(),
                ScriptPubKeyHex = scriptPubKey.ToBytes().ToHexString(),
                OtherPublicKeys = new System.Collections.Generic.Dictionary<string, string>(),
            };

            multiSigAddress.OtherPublicKeys.Add(otherPubKey.ToBytes().ToHexString(), "Bob");
               


            x1WalletFile.MultiSigAddresses[multiSigAddress.Address] = multiSigAddress;

        }

        internal static MultiSigAddress[] GetAllMultiSigAddresses(int skip, int? take, X1WalletFile x1WalletFile)
        {
            var store = x1WalletFile.MultiSigAddresses.Values;
            var filter = AllMultiSigAddresses; // TODO
            return take.HasValue
                ? store.Where(filter).Skip(skip).Take(take.Value).ToArray()
                : store.Where(filter).ToArray();
        }

        static readonly Func<MultiSigAddress, bool> AllMultiSigAddresses =
            (x) => x.AddressType == AddressType.MultiSig; // TODO
    }
}
