using System;
using System.Linq;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Features.ColdStaking;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Addresses
{
    static class ColdStakingAddressService
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

            Key hotPrivateKey = hotKey.GetKey(passphrase);
            PubKey hotPubKey = hotPrivateKey.PubKey.Compress();

            Script csRedeemScript = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKey.WitHash.AsKeyId(), coldPubKey.WitHash.AsKeyId());

            string csScriptAddress = csRedeemScript.WitHash.GetAddress(C.Network).ToString();

            Script csScriptAddressScriptPubKey = csRedeemScript.WitHash.ScriptPubKey;

            var csAddress = new ColdStakingAddress
            {
                ColdKey = coldKey,
                HotKey = hotKey,
                StakingKey = hotPrivateKey.ToBytes(),
                Label = "Default CS",
                AddressType = AddressType.ColdStakingHot, // TODO: which address type?
                Address = csScriptAddress,
                LastSeenHeight = null,
                ScriptPubKeyHex = csScriptAddressScriptPubKey.ToBytes().ToHexString(),
                RedeemScriptHex = csRedeemScript.ToBytes().ToHexString()
            };

            if (csAddress.AddressType == AddressType.ColdStakingHot)
                csAddress.StakingKey = hotPrivateKey.ToBytes();

            x1WalletFile.ColdStakingAddresses[csAddress.Address] = csAddress;

        }

        internal static ColdStakingAddress[] GetAllColdStakingAddresses(int skip, int? take, X1WalletFile x1WalletFile)
        {
            var store = x1WalletFile.ColdStakingAddresses.Values;
            var filter = AllColdStakingAddresses; // TODO
            return take.HasValue
                ? store.Where(filter).Skip(skip).Take(take.Value).ToArray()
                : store.Where(filter).ToArray();
        }

        static readonly Func<ColdStakingAddress, bool> AllColdStakingAddresses =
            (x) => x.AddressType == AddressType.ColdStakingCold || x.AddressType == AddressType.ColdStakingHot; // TODO
    }
}
