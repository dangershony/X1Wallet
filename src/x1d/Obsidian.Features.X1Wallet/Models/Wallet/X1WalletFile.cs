using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Tools;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Models.Wallet
{
    public sealed class X1WalletFile
    {
        public const string FileExtension = ".x1wallet.json";

        public X1WalletFile()
        {
            this.PubKeyHashAddresses = new Dictionary<string, PubKeyHashAddress>();
            this.MultiSigAddresses = new Dictionary<string, MultiSigAddress>();
            this.ColdStakingAddresses = new Dictionary<string, ColdStakingAddress>();
        }

        public int Version { get; set; }

        public string WalletName { get; set; }

        public string Comment { get; set; }

        /// <summary>
        /// The BIP-0044 Coin Type or 0 if not defined.
        /// </summary>
        public int CoinType { get; set; }

        /// <summary>
        /// A string to identify the network, e.g. ODX, tODX, BTC, tBTC.
        /// </summary>
        public string CoinTicker { get; set; }

        /// <summary>
        /// The WalletGuid correlates the X1WalletFile and the X1WalletMetadataFile.
        /// </summary>
        public Guid WalletGuid { get; set; }

        public long CreatedUtc { get; set; }

        public long ModifiedUtc { get; set; }

        public long? LastBackupUtc { get; set; }

        public byte[] PassphraseChallenge { get; set; }

        public byte[] HdSeed { get; set; }

        public bool HdSeedHasBip39Passphrase { get; set; }

        public Dictionary<string, PubKeyHashAddress> PubKeyHashAddresses { get; set; } 

        public Dictionary<string, ColdStakingAddress> ColdStakingAddresses { get; set; }

        public Dictionary<string, MultiSigAddress> MultiSigAddresses { get; set; }

      
        public ISegWitAddress FindAddress(string bech32)
        {
            if (this.PubKeyHashAddresses.TryGetValue(bech32, out var pubKeyHashAddress))
                return pubKeyHashAddress;
            if (this.ColdStakingAddresses.TryGetValue(bech32, out var coldStakingAddress))
                return coldStakingAddress;
            if (this.MultiSigAddresses.TryGetValue(bech32, out var multiSigAddress))
                return multiSigAddress;
            return null;
        }

        public PubKeyHashAddress GetReceiveAddress(bool canFallbackToUsedAddress, string passphrase = null)
        {
            var unusedReceiveAddress = this.PubKeyHashAddresses.Values.FirstOrDefault(x => x.KeyMaterial.IsChange == 0 && x.FirstSeenUtc == null);
            if (unusedReceiveAddress != null)
                return unusedReceiveAddress;
            if (canFallbackToUsedAddress)
            {
                var usedReceiveAddress =
                    this.PubKeyHashAddresses.Values.FirstOrDefault(x => x.KeyMaterial.IsChange == 0);
                if (usedReceiveAddress != null)
                    return usedReceiveAddress;
            }
            if (string.IsNullOrWhiteSpace(passphrase))
                throw new X1WalletException(System.Net.HttpStatusCode.NotFound,
                    $"No receive address available, please retry with passphrase.");
            return CreateNewAddresses(isChange: 0, passphrase, addressesToCreate: 2)[0];
        }

        public PubKeyHashAddress GetChangeAddress(string passphrase, bool isSafeDummyForPreview)
        {
            if (isSafeDummyForPreview)
            {
                var anyAddressWeOwn =
                    this.PubKeyHashAddresses.Values.FirstOrDefault(x => x.KeyMaterial.IsChange == 1)
                    ?? this.PubKeyHashAddresses.Values.FirstOrDefault(x => x.KeyMaterial.IsChange == 0);
                if (anyAddressWeOwn != null)
                    return anyAddressWeOwn;

                if (string.IsNullOrWhiteSpace(passphrase))
                    throw new X1WalletException(System.Net.HttpStatusCode.InternalServerError,
                        $"No address available and no passphrase to create one.");
            }
            if (string.IsNullOrWhiteSpace(passphrase))
                throw new X1WalletException(System.Net.HttpStatusCode.BadRequest,
                    $"Missing the passphase to create an unused change address.");

            var unusedChangeAddresses =
                this.PubKeyHashAddresses.Values.Where(x => x.KeyMaterial.IsChange == 1).ToList();
            if (unusedChangeAddresses.Count <= 3)
                unusedChangeAddresses.AddRange(CreateNewAddresses(isChange: 0, passphrase, addressesToCreate: 2));
            return unusedChangeAddresses[0];

        }



        /// <summary>
        /// We assume that all PubKeyHashAddress addresses in this file are already used. Therefore this method
        /// should only be called when the wallet is fully up-to-date so that all used addresses are already discovered.
        /// </summary>
        public List<PubKeyHashAddress> CreateNewAddresses(int isChange, string passphrase, int addressesToCreate = 1)
        {
            var nextIndex = GetNextIndex(isChange);

            var created = 0;
            var newAddresses = new List<PubKeyHashAddress>(addressesToCreate);

            while (created < addressesToCreate)
            {
                var seed = VCL.DecryptWithPassphrase(passphrase, this.HdSeed);
                var keyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, this.CoinType, AddressType.PubKeyHash, isChange,
                    nextIndex);
                var privateKey = keyMaterial.GetKey(passphrase);
                var scriptPubKey = privateKey.PubKey.Compress().WitHash.ScriptPubKey;
                var address = new PubKeyHashAddress
                {
                    KeyMaterial = keyMaterial,
                    AddressType = AddressType.PubKeyHash,
                    Label = null,
                    ScriptPubKeyHex = scriptPubKey.ToHex()
                };
                address.Address = scriptPubKey.GetAddressFromScriptPubKey();
                Debug.Assert(address.Address.Length == C.PubKeyHashAddressLength);
                newAddresses.Add(address);
                this.PubKeyHashAddresses.Add(address.Address, address);
                created++;
                nextIndex++;
            }

            return newAddresses;
        }

        int GetNextIndex(int isChange)
        {
            var count = this.PubKeyHashAddresses.Values.Count(x =>
                x.KeyMaterial.KeyType == KeyType.Hd &&
                x.KeyMaterial.IsChange == isChange &&
                x.KeyMaterial.AddressIndex.HasValue);
            return count;
        }

    }
}
