using System;
using System.Collections.Generic;

namespace Obsidian.Features.X1Wallet.Models.Wallet
{
    public class P2WshAddress : IEquatable<P2WshAddress>, ISegWitAddress
    {
        /// <summary>
        /// Decrypts to the pure key, length 32 bytes.
        /// </summary>
        public byte[] EncryptedPrivateKey { get; set; }

        /// <summary>
        /// The compressed public key corresponding to the private key, length: 33 bytes.
        /// </summary>
        public byte[] CompressedPublicKey { get; set; }


        /// <summary>
        /// The string representation of the address.
        /// E.g. odx1qvar8r29r8llzj53q5utmcewpju59263h38250ws33lp2q45lmalqg5lmdd
        /// </summary>
        public string Address { get; set; }


        public AddressType AddressType { get; set; }
        public string ScriptPubKeyHex { get; }
        public string Label { get; set; }


        public string Description { get; set; }

        public PartnerPublicKey[] PartnerPublicKeys { get; set; }

        /// <summary>
        ///  In P2WSH payments, we refer to the hash of the Redeem Script as the scriptPubKey. It looks like:
        /// 0 674671a8a33ffe295220a717bc65c19728556a3789d547ba118fc2a0569fdf7e
        /// It is the hash of the redeem script.
        /// </summary>
        public byte[] ScriptPubKey { get; set; }

        /// <summary>
        /// The redeem script looks like:
        /// 1 03fad6426522dbda5c5a9f8cab24a54ccc374517ad8790bf7e5a14308afc1bf77b 0340ecf2e20978075a49369e35269ecf0651d2f48061ebbf918f3eb1964051f65c 2 OP_CHECKMULTISIG
        /// </summary>
        public byte[] RedeemScript { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as P2WshAddress);
        }

        public bool Equals(P2WshAddress other)
        {
            return other != null &&
                   this.Address == other.Address;
        }

        public override int GetHashCode()
        {
            return -1052816746 + EqualityComparer<string>.Default.GetHashCode(this.Address);
        }

        public static bool operator ==(P2WshAddress left, P2WshAddress right)
        {
            return EqualityComparer<P2WshAddress>.Default.Equals(left, right);
        }

        public static bool operator !=(P2WshAddress left, P2WshAddress right)
        {
            return !(left == right);
        }
    }

    public class PartnerPublicKey
    {
        public string Label;
        public byte[] CompressedPublicKey;
    }
}
