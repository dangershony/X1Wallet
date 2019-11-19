using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Features.ColdStaking;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Addresses
{
    static class Trash
    {

       



        static void EnsureAddressBuffer(X1WalletFile x1WalletFile)
        {
            //this.GetUnusedReceiveAddress()
            //this.X1WalletFile.CreateNewAddresses();
        }




        static bool IsAddressUsedInConfirmedTransactions(ISegWitAddress address, IReadOnlyCollection<BlockMetadata> blocks)
        {
            // slow version
            foreach (BlockMetadata block in blocks)
            {
                foreach (TransactionMetadata tx in block.Transactions)
                {
                    foreach (var utxo in tx.Received.Values)
                    {
                        if (utxo.Address == address.Address)
                            return true;
                    }

                }
            }
            return false;
        }
        //internal string EnsureDummyMultiSig1Of2Address()
        //{
        //    var passphrase = "passwordpassword";

        //    if (this.x1WalletFile.HdSeed == null)
        //    {
        //        var hdBytes = new byte[32];
        //        var Rng = new RNGCryptoServiceProvider();
        //        Rng.GetBytes(hdBytes);
        //        var wl = Wordlist.English;
        //        var mnemonic = new Mnemonic(wl, hdBytes);
        //        byte[] hdSeed = mnemonic.DeriveSeed("");
        //        this.x1WalletFile.HdSeed = VCL.EncryptWithPassphrase(passphrase, hdSeed);
        //    }


        //    var seed = VCL.DecryptWithPassphrase(passphrase, this.x1WalletFile.HdSeed);

        //    // own key
        //    KeyMaterial myKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, C.Network.Consensus.CoinType, AddressType.MultiSig, 0, 0);
        //    PubKey myPubKey = myKeyMaterial.GetKey(passphrase).PubKey.Compress();

        //    // other Key
        //    KeyMaterial otherKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, C.Network.Consensus.CoinType, AddressType.MultiSig, 0, 1);
        //    var otherPubKey = otherKeyMaterial.GetKey(passphrase).PubKey.Compress();

        //    // The redeem script looks like:
        //    // 1 03fad6426522dbda5c5a9f8cab24a54ccc374517ad8790bf7e5a14308afc1bf77b 0340ecf2e20978075a49369e35269ecf0651d2f48061ebbf918f3eb1964051f65c 2 OP_CHECKMULTISIG
        //    Script redeemScript = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(1, myPubKey, otherPubKey);

        //    // The address looks like:
        //    // odx1qvar8r29r8llzj53q5utmcewpju59263h38250ws33lp2q45lmalqg5lmdd
        //    string bech32ScriptAddress = redeemScript.WitHash.GetAddress(C.Network).ToString();

        //    // In P2SH payments, we refer to the hash of the Redeem Script as the scriptPubKey. It looks like:
        //    // 0 674671a8a33ffe295220a717bc65c19728556a3789d547ba118fc2a0569fdf7e
        //    Script scriptPubKey = redeemScript.WitHash.ScriptPubKey;
        //    var scp = scriptPubKey.ToString();

        //    throw new NotImplementedException();
        //    //if (this.X1WalletFile.ScriptAddresses == null)
        //    //    this.X1WalletFile.ScriptAddresses = new Dictionary<string, P2WshAddress>();

        //    //if (this.X1WalletFile.ScriptAddresses.ContainsKey(bech32ScriptAddress))
        //    //    this.X1WalletFile.ScriptAddresses.Remove(bech32ScriptAddress);

        //    //if (!this.X1WalletFile.ScriptAddresses.ContainsKey(bech32ScriptAddress))
        //    //{
        //    //    this.X1WalletFile.ScriptAddresses.Add(bech32ScriptAddress,
        //    //        new P2WshAddress
        //    //        {
        //    //            Address = bech32ScriptAddress,
        //    //            ScriptPubKey = scriptPubKey.ToBytes(),
        //    //            RedeemScript = redeemScript.ToBytes(),
        //    //            AddressType = AddressType.MultiSig,
        //    //            CompressedPublicKey = myPubKey.ToBytes(),
        //    //            EncryptedPrivateKey = myKeyMaterial.EncryptedPrivateKey,
        //    //            Description = "My and Bob's 1-of-2 MultiSig account",
        //    //            PartnerPublicKeys = new[]{ new PartnerPublicKey
        //    //            {
        //    //                Label = "Bob",
        //    //                CompressedPublicKey=otherPubKey.ToBytes()
        //    //            }}
        //    //        });
        //    //    this.X1WalletFile.SaveX1WalletFile(this.CurrentX1WalletFilePath);
        //    //}
        //    //return bech32ScriptAddress;
        //}

        //internal ColdStakingAddress EnsureColdStakingAddress(string passphrase)
        //{

        //    //if (this.x1WalletFile.ColdStakingAddresses != null && this.x1WalletFile.ColdStakingAddresses.Count > 0)
        //    //{
        //    //    return this.x1WalletFile.ColdStakingAddresses.Values.First();
        //    //}

        //    //this.x1WalletFile.ColdStakingAddresses = new Dictionary<string, ColdStakingAddress>();

        //    //var seed = VCL.DecryptWithPassphrase(passphrase, this.x1WalletFile.HdSeed);

        //    //KeyMaterial coldKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, C.Network.Consensus.CoinType, AddressType.ColdStakingCold, 0, 0);
        //    //PubKey coldPubKey = coldKeyMaterial.GetKey(passphrase).PubKey.Compress();

        //    //KeyMaterial hotKeyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, C.Network.Consensus.CoinType, AddressType.ColdStakingHot, 0, 0);
        //    //PubKey hotPubKey = hotKeyMaterial.GetKey(passphrase).PubKey.Compress();

        //    //Script csRedeemScript = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKey.WitHash.AsKeyId(), coldPubKey.WitHash.AsKeyId());

        //    //// In P2SH payments, we refer to the hash of the Redeem Script as the scriptPubKey. It looks like:
        //    //// 0 674671a8a33ffe295220a717bc65c19728556a3789d547ba118fc2a0569fdf7e
        //    //Script scriptPubKey = csRedeemScript.WitHash.ScriptPubKey;

        //    //string bech32ScriptAddress = csRedeemScript.WitHash.GetAddress(C.Network).ToString();



        //    //var scp = scriptPubKey.ToString();
        //    //throw new NotImplementedException();
        //    //if (this.X1WalletFile.ScriptAddresses == null)
        //    //    this.X1WalletFile.ScriptAddresses = new Dictionary<string, P2WshAddress>();

        //    //if (this.X1WalletFile.ScriptAddresses.ContainsKey(bech32ScriptAddress))
        //    //    this.X1WalletFile.ScriptAddresses.Remove(bech32ScriptAddress);

        //    //if (!this.X1WalletFile.ScriptAddresses.ContainsKey(bech32ScriptAddress))
        //    //{
        //    //    //this.X1WalletFile.ScriptAddresses.Add(bech32ScriptAddress,
        //    //    //    new P2WshAddress
        //    //    //    {
        //    //    //        Address = bech32ScriptAddress,
        //    //    //        ScriptPubKey = scriptPubKey.ToBytes(),
        //    //    //        RedeemScript = redeemScript.ToBytes(),
        //    //    //        AddressType = AddressType.HdMultiSig,
        //    //    //        CompressedPublicKey = myMultiSigPublicKeyBytes,
        //    //    //        EncryptedPrivateKey = myMultiSigPrivate.EncryptedPrivateKey,
        //    //    //        Description = "My and Bob's 1-of-2 MultiSig account",
        //    //    //        PartnerPublicKeys = new[]{ new PartnerPublicKey
        //    //    //        {
        //    //    //            Label = "Bob",
        //    //    //            CompressedPublicKey=partnerMultiSigPublicKeyBytes2
        //    //    //        }}
        //    //    //    });
        //    //    //this.X1WalletFile.SaveX1WalletFile(this.CurrentX1WalletFilePath);
        //    //}

        //    //var scpAsString = scriptPubKey.ToString();
        //    //var paymentScript = scriptPubKey.PaymentScript;
        //    //var paymentScriptAsString = paymentScript.ToString();

        //    //var pswsh = scriptPubKey.WitHash.ScriptPubKey;
        //    //var p2wshadr = scriptPubKey.WitHash.GetAddress(this.network);

        //    return new ColdStakingAddress();
        //}

    }
}
