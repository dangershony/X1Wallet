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
        internal static void EnsureDefaultColdStakingAddress(string passphrase, X1WalletFile x1WalletFile)
        {
            //if (x1WalletFile.ColdStakingAddresses.Count > 0)
            //    return;

            var decryptedSeed = VCL.DecryptWithPassphrase(passphrase, x1WalletFile.HdSeed);

            var coldKey = KeyHelper.CreateHdKeyMaterial(decryptedSeed, passphrase, C.Network.Consensus.CoinType, AddressType.ColdStakingCold,
                C.External, 0);

            var hotKey = KeyHelper.CreateHdKeyMaterial(decryptedSeed, passphrase, C.Network.Consensus.CoinType, AddressType.ColdStakingHot,
                C.External, 0);

            PubKey coldPubKey = coldKey.GetKey(passphrase).PubKey.Compress();
            PubKey hotPubKey = hotKey.GetKey(passphrase).PubKey.Compress();

            Script csRedeemScript = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKey.WitHash.AsKeyId(), coldPubKey.WitHash.AsKeyId());

            string csScriptAddress = csRedeemScript.WitHash.GetAddress(C.Network).ToString();

            Script csScriptAddressScriptPubKey = csRedeemScript.WitHash.ScriptPubKey;

            var csAddress = new ColdStakingAddress
            {
                ColdKey = coldKey,
                HotKey = hotKey,
                Label = "Default CS",
                AddressType = AddressType.MultiSig, // TODO: which address type?
                Address = csScriptAddress,
                LastSeenHeight = null,
                ScriptPubKeyHex = csScriptAddressScriptPubKey.ToBytes().ToHexString(),
                RedeemScriptHex = csRedeemScript.ToBytes().ToHexString()
            };

            x1WalletFile.ColdStakingAddresses[csAddress.Address] = csAddress;

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
