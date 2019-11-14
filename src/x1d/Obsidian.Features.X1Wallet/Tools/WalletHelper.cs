using System;
using System.Collections.Generic;
using System.IO;
using NBitcoin;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Stratis.Bitcoin.Configuration;

namespace Obsidian.Features.X1Wallet.Tools
{
    public static class WalletHelper
    {
        public static string GetX1WalletFilepath(this string walletName, Network network, DataFolder dataFolder)
        {
            if (string.IsNullOrWhiteSpace(walletName))
                throw new ArgumentNullException(nameof(walletName));

            var fileName = $"{walletName}.{network.CoinTicker}{X1WalletFile.FileExtension}";
            string filePath = Path.Combine(dataFolder.WalletPath, fileName);
            return filePath;
        }

        public static string GetX1WalletMetaDataFilepath(this string walletName, Network network, DataFolder dataFolder)
        {
            if (string.IsNullOrWhiteSpace(walletName))
                throw new ArgumentNullException(nameof(walletName));

            var fileName = $"{walletName}.{network.CoinTicker}{X1WalletMetadataFile.FileExtension}";
            string filePath = Path.Combine(dataFolder.WalletPath, fileName);
            return filePath;
        }

        public static void SaveX1WalletFile(this X1WalletFile x1WalletFile, string filePath)
        {
            var serializedWallet = Serializer.Serialize(x1WalletFile);
            File.WriteAllText(filePath, serializedWallet);
        }

        public static X1WalletFile LoadX1WalletFile(string filePath)
        {
            var file = File.ReadAllText(filePath);
            var x1WalletFile = Serializer.Deserialize<X1WalletFile>(file);
            if (Path.GetFileName(filePath.Replace(X1WalletFile.FileExtension, string.Empty)
                    .Replace($".{x1WalletFile.CoinTicker}", string.Empty)) != x1WalletFile.WalletName)
                throw new InvalidOperationException(
                    $"The wallet name {x1WalletFile.WalletName} inside of file {filePath} doesn't match the naming convention for {X1WalletFile.FileExtension}-files. Please correct this.");

            // a good place for file format updates is here
            if (x1WalletFile.Version == C.WalletKeyFileVersion)
                return x1WalletFile;

            if (x1WalletFile.Version > C.WalletKeyFileVersion)
                throw new X1WalletException(System.Net.HttpStatusCode.ExpectationFailed,
                    $"The version of the wallet file {filePath} is {x1WalletFile.Version}, but this program supports only version {C.WalletKeyFileVersion}. Please use the program version that you have used to create the wallet.");

            // create a backup before update
            var backupPath = $"{filePath}.v{x1WalletFile.Version}.bak";
            x1WalletFile.SaveX1WalletFile(backupPath);

            Update(x1WalletFile);
            x1WalletFile.Version = C.WalletKeyFileVersion;
            SaveX1WalletFile(x1WalletFile, filePath);
            return x1WalletFile;
        }

        static void Update(X1WalletFile conversion)
        {
            throw new X1WalletException(System.Net.HttpStatusCode.InternalServerError,
                $"An upgrade from version {conversion.Version} to a version {C.WalletKeyFileVersion} is not supported :-/");

            #region sample backup strategy that cannot be compiled any more

            //foreach (var p2WpkhAddress in conversion.Addresses.Values)
            //{
            //    var keyMaterial = new KeyMaterial
            //    {
            //        EncryptedPrivateKey = p2WpkhAddress.EncryptedPrivateKey,
            //        CreatedUtc = DateTime.UtcNow.AddYears(-1).ToUnixTime(),
            //        AddressIndex = null,
            //        IsChange = null,
            //        KeyPath = null,
            //        KeyType = KeyType.Generated
            //    };

            //    var scriptPubKey = new PubKey(p2WpkhAddress.CompressedPublicKey).WitHash.ScriptPubKey;

            //    var pubKeyHashAddress = new PubKeyHashAddress
            //    {
            //        KeyMaterial = keyMaterial,
            //        AddressType = AddressType.PubKeyHash,
            //        FirstSeenUtc = null,
            //        Label = null,
            //        ScriptPubKeyHex = scriptPubKey.ToBytes().ToHexString(),
            //        Address = scriptPubKey.GetAddressFromScriptPubKey()
            //    };

            //    conversion.PubKeyHashAddresses.Add(pubKeyHashAddress.Address, pubKeyHashAddress);
            //}

            //foreach (var scriptAddress in conversion.ScriptAddresses.Values)
            //{
            //    var keyMaterial = new KeyMaterial
            //    {
            //        EncryptedPrivateKey = scriptAddress.EncryptedPrivateKey,
            //        CreatedUtc = DateTime.UtcNow.AddYears(-1).ToUnixTime(),
            //        AddressIndex = null,
            //        IsChange = null,
            //        KeyPath = null,
            //        KeyType = KeyType.Generated
            //    };

            //    var ms = new MultiSigAddress
            //    {
            //        OwnKey = keyMaterial,
            //        MaxSignatures = 2,
            //        SignaturesRequired = 1,
            //        AddressType = AddressType.MultiSig,
            //        Label = scriptAddress.Description,
            //        Address = scriptAddress.Address,
            //        RedeemScriptHex = scriptAddress.RedeemScript.ToHexString(),
            //        ScriptPubKeyHex = scriptAddress.ScriptPubKey.ToHexString(),
            //        OtherPublicKeys = new Dictionary<string, string>()
            //    };
            //    Debug.Assert(scriptAddress.PartnerPublicKeys.Length == 1);
            //    ms.OtherPublicKeys.Add(scriptAddress.PartnerPublicKeys[0].CompressedPublicKey.ToHexString(),
            //        scriptAddress.PartnerPublicKeys[0].Label);

            //    conversion.MultiSigAddresses.Add(ms.Address, ms);
            //}
            //conversion.ScriptAddresses = null;
            //conversion.Addresses = null;

            #endregion
        }

        public static X1WalletMetadataFile CreateX1WalletMetadataFile(this X1WalletFile x1WalletFile,
            uint256 genesisHash)
        {
            return new X1WalletMetadataFile
            {
                X1WalletAssemblyVersion = GetX1WalletAssemblyVersion(),
                WalletGuid = x1WalletFile.WalletGuid,
                CheckpointHash = genesisHash,
                SyncedHash = genesisHash,
                Blocks = new Dictionary<int, BlockMetadata>(),
                MemoryPool = new MemoryPoolMetadata { Entries = new HashSet<MemoryPoolEntry>() }
            };
        }

        public static X1WalletMetadataFile LoadOrCreateX1WalletMetadataFile(string x1WalletMetadataFilePath,
            X1WalletFile x1WalletFile, uint256 genesisHash)
        {
            X1WalletMetadataFile x1WalletMetadataFile;
            if (File.Exists(x1WalletMetadataFilePath))
            {
                x1WalletMetadataFile =
                    Serializer.Deserialize<X1WalletMetadataFile>(File.ReadAllText(x1WalletMetadataFilePath));
                if (x1WalletMetadataFile.X1WalletAssemblyVersion == GetX1WalletAssemblyVersion() &&
                    x1WalletMetadataFile.WalletGuid == x1WalletFile.WalletGuid)
                    return x1WalletMetadataFile;
                return CreateX1WalletMetadataFile(x1WalletFile, genesisHash);

            }

            x1WalletMetadataFile = x1WalletFile.CreateX1WalletMetadataFile(genesisHash);
            x1WalletMetadataFile.SaveX1WalletMetadataFile(x1WalletMetadataFilePath);
            return x1WalletMetadataFile;
        }

        public static void SaveX1WalletMetadataFile(this X1WalletMetadataFile x1WalletMetadataFile, string filePath)
        {
            var serializedWallet = Serializer.Serialize(x1WalletMetadataFile);
            File.WriteAllText(filePath, serializedWallet);
        }

        public static DateTime ToDateTimeUtc(this long unixTimeSeconds)
        {
            if (unixTimeSeconds >= 4102444800)
            {
                throw new ArgumentOutOfRangeException(nameof(unixTimeSeconds), unixTimeSeconds,
                    "A date of 1st Jan 2100 or later is not plausible.");
            }

            return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).UtcDateTime;
        }

        public static long ToUnixTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                throw new InvalidOperationException(
                    $"Expected DateTimeKind.Utc but {nameof(utcDateTime)}.Kind was {utcDateTime.Kind}");
            }

            return new DateTimeOffset(utcDateTime).ToUnixTimeSeconds();
        }

        static string GetX1WalletAssemblyVersion()
        {
            return typeof(X1WalletMetadataFile).Assembly.GetShortVersionString();
        }
    }
}
