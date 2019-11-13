using System;
using System.Collections.Generic;
using Obsidian.Features.X1Wallet.Tools;

namespace Obsidian.Features.X1Wallet.Models.Wallet
{
    public interface ISegWitAddress
    {
        /// <summary>
        /// The Bech32 representation of the address. Unique key of the address across all types of ISegWitAddress for dictionaries and
        /// comparisons. Use ordinal case sensitive comparison. Ensure the string is lowercase.
        /// </summary>
        string Address { get; }
        AddressType AddressType { get; }
        string ScriptPubKeyHex { get; }
        string Label { get; set; }
    }

    public interface ISegWitScriptAddress
    {
        string RedeemScriptHex { get; }
    }

    /// <summary>
    /// The AddressType defines what kind of address we are dealing with (e.g. P2WPK or what type
    /// of script address it is. Also, if the underlying key(s) are Hd, the enum values are used to define
    /// the Hd key path. See <see cref="KeyHelper.CreateDerivedPrivateKey"/> for the key path mappings.
    /// </summary>
    public enum AddressType : int
    {
        PubKeyHash = 0,
        MultiSig = 10,
        ColdStakingCold = 30,
        ColdStakingHot = 35
    }

    public sealed class KeyMaterial
    {
        public KeyType KeyType;

        public string KeyPath;

        public int? AddressIndex;

        public int? IsChange;

        public DateTime CreatedUtc;

        public byte[] EncryptedPrivateKey;
    }

    public enum KeyType
    {
        NotSet = 0,
        Hd = 10,
        Generated = 20,
        Imported = 30
    }

    public sealed class PubKeyHashAddress : ISegWitAddress
    {
        public string Address { get; set; }

        public AddressType AddressType { get; set; }

        public string ScriptPubKeyHex { get; set; }

        public string Label { get; set; }

        /// <summary>
        /// This property must only be set while processing transactions from the blockchain.
        /// The presence of a valid date indicates that the address is a used address.
        /// </summary>
        public DateTime? FirstSeenUtc { get; set; }

        public KeyMaterial KeyMaterial;

    }

    public sealed class MultiSigAddress : ISegWitAddress, ISegWitScriptAddress
    {
        public AddressType AddressType { get; set; }

        public string Address { get; set; }

        public string ScriptPubKeyHex { get; set; }

        public string Label { get; set; }

        public string RedeemScriptHex { get; set; }

        public KeyMaterial OwnKey { get; set; }

        public int SignaturesRequired { get; set; }

        public int MaxSignatures { get; set; }

        /// <summary>
        /// Key: Compressed public key bytes as lowercase hex string.
        /// Value: Nickname of the owner of the public key for display.
        /// </summary>
        public Dictionary<string,string> OtherPublicKeys { get; set; }
    }

    public class ColdStakingAddress : ISegWitAddress, ISegWitScriptAddress
    {
        public string Address { get; set; }

        public AddressType AddressType { get; set; }

        public string ScriptPubKeyHex { get; set; }

        public string Label { get; set; }

        public string RedeemScriptHex { get; set; }

        public KeyMaterial ColdKey { get; set; }

        public KeyMaterial HotKey { get; set; }
    }
}
