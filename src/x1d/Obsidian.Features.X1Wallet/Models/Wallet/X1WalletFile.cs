using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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



        /// <summary>
        /// TODO: Ensure that also addresses that appear in the memory pool are marked as 'used'.
        /// </summary>
        public PubKeyHashAddress GetChangeAddress(string passphrase, bool isSafeDummyForPreview)
        {
            var store = this.PubKeyHashAddresses.Values;

            // If we have an existing unused change address, we just use it.
            var existingUnusedChangeAddresses = store.Where(this.changeAddressesNeverSeenUsedOnTheChain).ToArray();
            if (existingUnusedChangeAddresses.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(passphrase))
                {
                    var surplus = existingUnusedChangeAddresses.Length - 1 - C.UnusedChangeAddressBuffer;
                    if (surplus < C.UnusedChangeAddressBuffer)
                    {
                        var refill = Math.Abs(surplus);
                        CreateAndInsertNewChangeAddresses(passphrase, refill);
                    }
                }
                return existingUnusedChangeAddresses[0];
            }


            if (isSafeDummyForPreview)
            {
                // If we just need an address to test the transaction or estimate the fee,
                // we never create a new change address (even if we could, because we have the passphrase).
                // For safety reasons, we'll use an address we own anyway.
                var anyAddressWeOwn = store.FirstOrDefault(x => x.KeyMaterial.IsChange == C.Change) // prefer a hd change address
                    ?? store.FirstOrDefault(x => x.KeyMaterial.IsChange == C.External &&
                                                 x.KeyMaterial.KeyType == KeyType.Hd) // then a hd external address
                    ?? store.FirstOrDefault(x => x.KeyMaterial.IsChange == C.External &&
                                                 (x.KeyMaterial.KeyType == KeyType.Generated || x.KeyMaterial.KeyType == KeyType.Imported)); // then non-hd
                if (anyAddressWeOwn != null)
                    return anyAddressWeOwn;

                throw new X1WalletException(System.Net.HttpStatusCode.InternalServerError,
                    $"Neither an unused change address or a dummy address is available.");
            }

            // If we are here, it means we really have to create a new address to satisfy the request,
            // and in that case, the passphrase is required.
            if (string.IsNullOrWhiteSpace(passphrase))
                throw new X1WalletException(System.Net.HttpStatusCode.BadRequest,
                    $"Missing the passphase to create an unused change address.");

            var newChangeAddresses = CreateAndInsertNewChangeAddresses(passphrase, addressesToCreate: C.UnusedChangeAddressBuffer);

            return newChangeAddresses[0];
        }



        /// <summary>
        /// We assume that all PubKeyHashAddress addresses in this file are already used. Therefore this method
        /// should only be called when the wallet is fully up-to-date so that all used addresses are already discovered.
        /// </summary>
        public List<PubKeyHashAddress> CreateAndInsertNewChangeAddresses(string passphrase, int addressesToCreate)
        {
            var seed = VCL.DecryptWithPassphrase(passphrase, this.HdSeed);

            var nextIndex = GetNextIndex(C.Change);

            var created = 0;
            var newAddresses = new List<PubKeyHashAddress>(addressesToCreate);

            while (created < addressesToCreate)
            {
                var keyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, this.CoinType, AddressType.PubKeyHash, C.Change,
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

        /// <summary>
        /// Wallet must be in perfect state, must be called locked.
        /// </summary>
        public PubKeyHashAddress CreateAndInsertNewReceiveAddress(string label, string passphrase)
        {
            if (!IsLabelUnique(label))
                throw new X1WalletException($"The label '{label}' is already in use");

            var nextIndex = GetNextIndex(C.External);

            var seed = VCL.DecryptWithPassphrase(passphrase, this.HdSeed);
            var keyMaterial = KeyHelper.CreateHdKeyMaterial(seed, passphrase, this.CoinType, AddressType.PubKeyHash, C.External, nextIndex);
            var privateKey = keyMaterial.GetKey(passphrase);
            var scriptPubKey = privateKey.PubKey.Compress().WitHash.ScriptPubKey;
            var address = new PubKeyHashAddress
            {
                KeyMaterial = keyMaterial,
                AddressType = AddressType.PubKeyHash,
                Label = label,
                ScriptPubKeyHex = scriptPubKey.ToHex()
            };
            address.Address = scriptPubKey.GetAddressFromScriptPubKey();
            Debug.Assert(address.Address.Length == C.PubKeyHashAddressLength);
            this.PubKeyHashAddresses.Add(address.Address, address); // throws if the address, and by extension, the index, already exists.
            return address;
        }

        public bool IsLabelUnique(string label)
        {
            foreach (var address in this.PubKeyHashAddresses.Values)
                if (address.Label == label)
                    return false;
            foreach (var address in this.MultiSigAddresses.Values)
                if (address.Label == label)
                    return false;
            foreach (var address in this.ColdStakingAddresses.Values)
                if (address.Label == label)
                    return false;

            return true;
        }

        int GetNextIndex(int isChange)
        {
            int[] indexesInUse = this.PubKeyHashAddresses.Values.Where(x =>
                x.KeyMaterial.KeyType == KeyType.Hd &&
                x.KeyMaterial.IsChange == isChange &&
                x.KeyMaterial.AddressIndex.HasValue).Select(x => x.KeyMaterial.AddressIndex.Value).ToArray();
            var count = indexesInUse.Length;

            var max = 0;
            if (count > 0)
                max = indexesInUse.Max();

            var next = Math.Max(count, max);

            if (count != max)
                Log.Logger.LogWarning(
                    $"For addresses oy type {isChange}, the count of {count} does not equal max of {max}. Using index {next} for the next address to be created.");
            return next;

        }

        [JsonIgnore]
        readonly Func<PubKeyHashAddress, bool> changeAddressesNeverSeenUsedOnTheChain = (x) => x.KeyMaterial.KeyType == KeyType.Hd &&   // must be hd
                                                                                      x.KeyMaterial.AddressIndex.HasValue &&   // if valid hd, this must have a value
                                                                                      x.KeyMaterial.IsChange == C.Change &&    // we require it's a change address 
                                                                                      x.FirstSeenUtc == null;
        [JsonIgnore]
        public readonly Func<PubKeyHashAddress, bool> allPubKeyHashReceiveAddresses = (x) => x.AddressType == AddressType.PubKeyHash &&
                                                                                      x.KeyMaterial.IsChange != C.Change;
    }
}
