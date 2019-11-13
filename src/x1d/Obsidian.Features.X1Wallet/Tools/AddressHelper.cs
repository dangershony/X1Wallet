using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models.Wallet;
using VisualCrypt.VisualCryptLight;
using VisualCrypt.VisualCryptLight.VisualCryptLib.Api.Infrastructure;

namespace Obsidian.Features.X1Wallet.Tools
{
    public static class AddressHelper
    {
        public const int Bech32PubKeyAddressLenght = 43;
        public const int Bech32ScriptAddressLenght = 63;

        static Bech32Encoder _pubKeyAddressEncoder;
        static Bech32Encoder _scriptAddressEncoder;
        static string _hrp1;
        static int _coinType;

        public static void Init(Network network)
        {
            _pubKeyAddressEncoder = network.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS];
            _scriptAddressEncoder = network.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS];
            _hrp1 = Encoding.ASCII.GetString(_pubKeyAddressEncoder.HumanReadablePart) + "1";
            _coinType = network.Consensus.CoinType;
        }

        public static Script ScriptPubKeyFromPublicKey(this ISegWitAddress address)
        {
            
            //if (address.AddressType == AddressType.HdAddress || address.AddressType == AddressType.HdChangeAddress ||
            //    address.AddressType == AddressType.SingleKey)
            //{
            //    KeyHelper.CheckBytes(address.CompressedPublicKey, 33);
            //    var hash160 = Hashes.Hash160(address.CompressedPublicKey).ToBytes();
            //    return new Script(OpcodeType.OP_0, Op.GetPushOp(hash160));
            //}

            return address.Address.ScriptPubKeyFromBech32ScriptAddressSafe();

        }

        public static Script ScriptPubKeyFromBech32Safe(this string bech32PubKeyAddress)
        {
            if (bech32PubKeyAddress == null || !bech32PubKeyAddress.StartsWith(_hrp1))
                InvalidAddress(bech32PubKeyAddress);

            var hash160 = _pubKeyAddressEncoder.Decode(bech32PubKeyAddress, out var witnessVersion);
            KeyHelper.CheckBytes(hash160, 20);

            if (witnessVersion != 0)
                InvalidAddress(bech32PubKeyAddress);

            return new Script(OpcodeType.OP_0, Op.GetPushOp(hash160));
        }

        public static Script ScriptPubKeyFromBech32ScriptAddressSafe(this string bech32ScriptAddress)
        {
            if (bech32ScriptAddress == null || !bech32ScriptAddress.StartsWith(_hrp1))
                InvalidAddress(bech32ScriptAddress);

            var hash256 = _scriptAddressEncoder.Decode(bech32ScriptAddress, out var witnessVersion);
            KeyHelper.CheckBytes(hash256, 32);

            if (witnessVersion != 0)
                InvalidAddress(bech32ScriptAddress);

            return new Script(OpcodeType.OP_0, Op.GetPushOp(hash256));
        }

        /// <summary>
        /// This can be the witness commitment in the coinbase transaction or any burn.
        /// </summary>
        public static bool IsOpReturn(this Script scriptPubKey)
        {
            if (scriptPubKey != null)
                return scriptPubKey.Length > 0 && scriptPubKey.ToBytes()[0] == (byte)OpcodeType.OP_RETURN;
            throw InvalidScriptPubKey(null);
        }

        /// <summary>
        /// This can be the non-witness-commitment output in a coinbase transaction.
        /// </summary>
        public static bool IsEmpty(this Script scriptPubKey)
        {
            if (scriptPubKey != null)
                return scriptPubKey.Length == 0;
            throw InvalidScriptPubKey(null);
        }

        /// <summary>
        /// Returns a P2WPKH or P2WSH bech32 string, or throws (it does not return null).
        /// </summary>
        public static string CreateBech32AddressFromScriptPubKey(this Script scriptPubKey)
        {
            string address;

            if (scriptPubKey == null || scriptPubKey.Length == 0)
                throw InvalidScriptPubKey(scriptPubKey);

            byte[] raw = scriptPubKey.ToBytes();

            switch (scriptPubKey)
            {
                // P2WPKH
                case var _ when raw.Length == 22 && raw[0] == 0 && raw[1] == 20:
                    var hash160 = raw.Skip(2).Take(20).ToArray();
                    address = hash160.Bech32PubKeyAddressFromHash160();
                    break;
                // P2WSH
                case var _ when raw.Length == 34 && raw[0] == 0 && raw[1] == 32:
                    var hash256 = raw.Skip(2).Take(32).ToArray();
                    address = hash256.Bech32ScriptAddressFromHash256();
                    break;
                // everything else is unwanted, but log exactly what it was
                default:
                    throw InvalidScriptPubKey(scriptPubKey);
            }

            return address;
        }


        public static string Bech32PubKeyAddressFromHash160(this byte[] hash160)
        {
            KeyHelper.CheckBytes(hash160, 20);

            return _pubKeyAddressEncoder.Encode(0, hash160);
        }

        public static string Bech32ScriptAddressFromHash256(this byte[] hash256)
        {
            KeyHelper.CheckBytes(hash256, 32);

            return _scriptAddressEncoder.Encode(0, hash256);
        }

        static void InvalidAddress(string input, Exception innerException = null)
        {
            var message = $"Invalid address '{input ?? "null"}'.";
            throw new X1WalletException(System.Net.HttpStatusCode.BadRequest, message, innerException);
        }

        static X1WalletException InvalidScriptPubKey(Script input, Exception innerException = null)
        {
            var message = $"Invalid ScriptPubKey '{input?.ToString() ?? "null"}'.";
            return new X1WalletException(System.Net.HttpStatusCode.BadRequest, message, innerException);
        }

       

        public static P2WpkhAddress CreateWithPrivateKey(byte[] privateKey, string keyEncryptionPassphrase,
            AddressType addressType)
        {
            if (string.IsNullOrWhiteSpace(keyEncryptionPassphrase))
                throw new ArgumentException(nameof(CreateWithPrivateKey));

            KeyHelper.CheckBytes(privateKey, 32);

            var adr = new P2WpkhAddress();
            adr.EncryptedPrivateKey = VCL.EncryptWithPassphrase(keyEncryptionPassphrase, privateKey);

            var k = new Key(privateKey);
            adr.CompressedPublicKey = k.PubKey.Compress().ToBytes();
            var hash160 = Hashes.Hash160(adr.CompressedPublicKey).ToBytes();
            adr.Address = _pubKeyAddressEncoder.Encode(0, hash160);
            adr.AddressType = addressType;
            return adr;
        }


       


       
       


    }
}
