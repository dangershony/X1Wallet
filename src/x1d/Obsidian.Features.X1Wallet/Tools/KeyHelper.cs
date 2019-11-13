using System;
using System.Linq;
using System.Security.Cryptography;
using NBitcoin;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models.Wallet;
using VisualCrypt.VisualCryptLight;
using VisualCrypt.VisualCryptLight.VisualCryptLib.Api.Infrastructure;

namespace Obsidian.Features.X1Wallet.Tools
{
    public static class KeyHelper
    {
        static readonly long Started = DateTime.UtcNow.Ticks;

        public static KeyMaterial GenerateRandomKeyMaterial(string keyEncryptionPassphrase, int length)
        {
            return new KeyMaterial
            {
                EncryptedPrivateKey = VCL.EncryptWithPassphrase(keyEncryptionPassphrase, GetSomewhatImprovedRandom(length)),
                KeyPath = null,
                AddressIndex = null,
                IsChange = null,
                CreatedUtc = DateTime.UtcNow,
                KeyType = KeyType.Generated
            };
        }

        public static KeyMaterial ImportKeyMaterial(byte[] privateKey, string keyEncryptionPassphrase)
        {
            CheckBytes(privateKey, 32);

            if (string.IsNullOrWhiteSpace(keyEncryptionPassphrase))
                throw new ArgumentException(nameof(keyEncryptionPassphrase));

            return new KeyMaterial
            {
                EncryptedPrivateKey = VCL.EncryptWithPassphrase(keyEncryptionPassphrase, privateKey),
                KeyPath = null,
                AddressIndex = null,
                IsChange = null,
                CreatedUtc = DateTime.UtcNow,
                KeyType = KeyType.Imported
            };
        }

        public static KeyMaterial CreateHdKeyMaterial(byte[] seed, string keyEncryptionPassphrase, int coinType,
            AddressType addressType, int? isChangeOrInternal, int addressIndex)
        {
            CheckBytes(seed, 64);

            if (string.IsNullOrWhiteSpace(keyEncryptionPassphrase))
                throw new ArgumentException(nameof(keyEncryptionPassphrase));

            var privateExtKey = CreateDerivedPrivateKey(seed, addressIndex, addressType, isChangeOrInternal, coinType, out string keyDerivationPath);
            var privateKeyBytes = privateExtKey.ToBytes();

            CheckBytes(privateKeyBytes, 32);

            var keyMaterial = new KeyMaterial
            {
                EncryptedPrivateKey = VCL.EncryptWithPassphrase(keyEncryptionPassphrase, privateKeyBytes),
                IsChange = isChangeOrInternal,
                AddressIndex = addressIndex,
                CreatedUtc = DateTime.UtcNow,
                KeyPath = keyDerivationPath,
                KeyType = KeyType.Hd
            };
            return keyMaterial;
        }

        public static Key GetKey(this KeyMaterial keyMaterial, string passphrase)
        {
            return new Key(VCL.DecryptWithPassphrase(passphrase, keyMaterial.EncryptedPrivateKey));
        }

        static Key CreateDerivedPrivateKey(byte[] seed, int addressIndex, AddressType addressType, int? isChangeOrInternal, int coinType, out string keyDerivationPath)
        {
            CheckBytes(seed, 64);

            if (addressIndex < 0)
                throw new X1WalletException(System.Net.HttpStatusCode.BadRequest, "Address Index must be >= 0.");

            if (isChangeOrInternal == null)
                throw new X1WalletException(System.Net.HttpStatusCode.BadRequest,
                    $"You need to specify {nameof(isChangeOrInternal)}, since that determines the key derivation path.");

            const int accountIndex = 0;

            if (addressType < 0)
                throw new X1WalletException(System.Net.HttpStatusCode.BadRequest,
                    $"{nameof(addressType)} maps to {(int)addressType} which can't be used as Hd account.");

            if (coinType == 0)
                throw new X1WalletException(System.Net.HttpStatusCode.BadRequest, "'0' is a suspicious CoinType, do not confuse with BIP-32 default account.");

            bool hardenFullKeyPath;
            int purpose;
            const int purposeBip44Compliant = 44;
            switch (addressType)
            {
                case AddressType.PubKeyHash:
                    hardenFullKeyPath = false;
                    purpose = purposeBip44Compliant; // m/44'
                    break;
                case AddressType.MultiSig:
                    hardenFullKeyPath = true;
                    purpose = purposeBip44Compliant + (int)addressType; // m/54'
                    break;
                case AddressType.ColdStakingCold:
                    hardenFullKeyPath = true;
                    purpose = purposeBip44Compliant + (int)addressType; // m/74'
                    break;
                case AddressType.ColdStakingHot:
                    hardenFullKeyPath = true;
                    purpose = purposeBip44Compliant + (int)addressType; // m/79'
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(addressType), addressType, null);
            }

            var keyPath = hardenFullKeyPath
                ? GetHardenedDerivationKeyPath(purpose, coinType, accountIndex, isChangeOrInternal.Value, addressIndex)
                : GetPublicDerivationKeyPath(purpose, coinType, accountIndex, isChangeOrInternal.Value, addressIndex);

            var seedExtKey = new ExtKey(seed);
            ExtKey privateExtKey = seedExtKey.Derive(keyPath);
            keyDerivationPath = keyPath.ToString();
            return privateExtKey.PrivateKey;
        }

        /// <summary>
        /// We define the following 5 levels in BIP32 path:
        /// m / purpose' / coin_type' / account' / change / address_index
        /// Purpose is a constant set to 44' (or 0x8000002C) following the BIP43 recommendation.
        /// It indicates that the subtree of this node is used according to this specification.
        /// Note that the Bitcoin Core wallet doesn't conform to this spec and uses instead:
        /// m / 0' /0' /4'
        /// <see>
        /// <cref>https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki</cref>
        /// <cref>https://bitcoin.stackexchange.com/questions/37488/eli5-whats-the-difference-between-a-child-key-and-a-hardened-child-key-in-bip3</cref>
        /// </see>
        /// </summary>
        static KeyPath GetHardenedDerivationKeyPath(int purpose, int coinType, int accountIndex, int changePath, int addressIndex)
        {
            return new KeyPath($"m/{purpose}'/{coinType}'/{accountIndex}'/{changePath}'/{addressIndex}'");
        }

        static KeyPath GetPublicDerivationKeyPath(int purpose, int coinType, int accountIndex, int changePath, int addressIndex)
        {
            return new KeyPath($"m/{purpose}'/{coinType}'/{accountIndex}'/{changePath}/{addressIndex}");
        }


        public static byte[] GetSomewhatImprovedRandom(int length)
        {
            if (length != 32 && length != 64)
                throw new ArgumentException(nameof(length));

            using var sha = SHA512.Create();
            using var rng = new RNGCryptoServiceProvider();

            var rngBytes = new byte[512];
            var tickBytes = BitConverter.GetBytes(DateTime.Now.Ticks);
            var startedTickBytes = BitConverter.GetBytes(Started * Started);
            var guidBytes = Guid.NewGuid().ToByteArray();

            rng.GetBytes(rngBytes);

            var sources = new[] { startedTickBytes, tickBytes, guidBytes, rngBytes };
            byte[] entropy = ByteArrays.Concatenate(rngBytes, startedTickBytes, tickBytes, guidBytes, rngBytes);

            foreach (var src in sources)
                entropy = sha.ComputeHash(ByteArrays.Concatenate(entropy, src));

            var ret = new byte[length];
            Buffer.BlockCopy(entropy, 0, ret, 0, length);
            return ret;
        }

        public static void CheckBytes(byte[] bytes, int expectedLength)
        {
            if (bytes == null || bytes.Length != expectedLength || bytes.All(b => b == bytes[0]))
            {
                var display = bytes == null ? "null" : bytes.ToHexString();
                var message =
                    $"Suspicious byte array '{display}', it does not look like a cryptographic key or hash, please investigate. Expected lenght was {expectedLength}.";
                throw new X1WalletException(System.Net.HttpStatusCode.BadRequest, message);
            }
        }
    }
}
