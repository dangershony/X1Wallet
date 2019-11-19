using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Addresses
{
    static class AddressService
    {

        /// <summary>
        /// TODO: Ensure that also addresses that appear in the memory pool are marked as 'used'.
        /// </summary>
        public static PubKeyHashAddress GetChangeAddress(string passphrase, bool isSafeDummyForPreview, X1WalletFile x1WalletFile)
        {
            if (!string.IsNullOrWhiteSpace(passphrase))
                CreateChangeAddressesIfNeeded(passphrase, x1WalletFile);

            var store = x1WalletFile.PubKeyHashAddresses.Values;

            // If we have an existing unused change address, we just use it.
            var existingUnusedChangeAddresses = store.Where(ChangeAddressesNeverSeenUsedOnTheChain).ToArray();
            if (existingUnusedChangeAddresses.Length > 0)
            {
                return existingUnusedChangeAddresses[0];
            }

            if (isSafeDummyForPreview)
            {
                // If we just need an address to test the transaction or estimate the fee,
                // we never create a new change address (even if we could, because we have the passphrase).
                // For safety reasons, we'll use an address we own anyway.
                var anyAddressWeOwn
                    = store.FirstOrDefault(x => x.KeyMaterial.IsChange == C.Change && x.KeyMaterial.KeyType == KeyType.Hd) // prefer a hd change address
                    ?? store.FirstOrDefault(x => x.KeyMaterial.IsChange == C.External && x.KeyMaterial.KeyType == KeyType.Hd) // then a hd external address
                    ?? store.FirstOrDefault(x => x.KeyMaterial.KeyType == KeyType.Generated || x.KeyMaterial.KeyType == KeyType.Imported); // then non-hd

                if (anyAddressWeOwn != null)
                    return anyAddressWeOwn;

                throw new X1WalletException(HttpStatusCode.InternalServerError, "No address available. To recover from this error, please unlock your wallet so that addresses can be generated.");
            }

            throw new X1WalletException("No unused change address available, please unlock you wallet so that we can create one.");
        }

        static void CreateChangeAddressesIfNeeded(string passphrase, X1WalletFile x1WalletFile)
        {
            if (passphrase == null)
                throw new ArgumentNullException(nameof(passphrase));

            var store = x1WalletFile.PubKeyHashAddresses.Values;
            var existingUnusedChangeAddresses = store.Where(ChangeAddressesNeverSeenUsedOnTheChain).ToArray();
            var surplus = existingUnusedChangeAddresses.Length - 1 - C.UnusedChangeAddressBuffer;
            if (surplus != 0 && surplus < C.UnusedChangeAddressBuffer)
            {
                var refill = Math.Abs(surplus);
                CreateAndInsertNewChangeAddresses(passphrase, refill, x1WalletFile);
            }
        }

        /// <summary>
        /// We assume that all PubKeyHashAddress addresses in this file are already used. Therefore this method
        /// should only be called when the wallet is fully up-to-date so that all used addresses are already discovered.
        /// </summary>
        public static List<PubKeyHashAddress> CreateAndInsertNewChangeAddresses(string passphrase, int addressesToCreate, X1WalletFile x1WalletFile)
        {
            var decryptedSeed = VCL.DecryptWithPassphrase(passphrase, x1WalletFile.HdSeed);

            var nextIndex = GetNextIndex(C.Change, x1WalletFile, out var _);

            var created = 0;
            var newAddresses = new List<PubKeyHashAddress>(addressesToCreate);

            while (created < addressesToCreate)
            {
                var address = GeneratePubKeyHashAddress(decryptedSeed, passphrase, x1WalletFile.CoinType, C.Change, nextIndex);
                newAddresses.Add(address);
                if (!x1WalletFile.PubKeyHashAddresses.TryAdd(address.Address, address))
                    Log.Logger.LogWarning($"Change address {address.Address} already existed - nothing was added.");
                created++;
                nextIndex++;
            }

            x1WalletFile.SaveX1WalletFile(x1WalletFile.CurrentPath);
            return newAddresses;
        }

        /// <summary>
        /// Wallet must be in perfect state, must be called locked.
        /// </summary>
        public static PubKeyHashAddress CreateAndInsertNewReceiveAddress(string label, string passphrase, X1WalletFile x1WalletFile)
        {
            if (!IsLabelUnique(label, x1WalletFile))
                throw new X1WalletException($"The label '{label}' is already in use");

            var nextIndex = GetNextIndex(C.External, x1WalletFile, out var _);

            var decryptedSeed = VCL.DecryptWithPassphrase(passphrase, x1WalletFile.HdSeed);
            var address = GeneratePubKeyHashAddress(decryptedSeed, passphrase, x1WalletFile.CoinType, C.External, nextIndex);
           
            if (!x1WalletFile.PubKeyHashAddresses.TryAdd(address.Address, address))
            {
                throw new X1WalletException(HttpStatusCode.InternalServerError, $"Receive address {address.Address} already exists.");
            }

            x1WalletFile.SaveX1WalletFile(x1WalletFile.CurrentPath);
            return address;
        }

        static PubKeyHashAddress GeneratePubKeyHashAddress(byte[] decryptedSeed, string passphrase, int coinType, int isChange, int index)
        {
            var keyMaterial = KeyHelper.CreateHdKeyMaterial(decryptedSeed, passphrase, coinType, AddressType.PubKeyHash, isChange, index);
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
            return address;
        }


        public static bool IsLabelUnique(string label, X1WalletFile x1WalletFile)
        {
            foreach (var address in x1WalletFile.PubKeyHashAddresses.Values)
                if (address.Label == label)
                    return false;
            foreach (var address in x1WalletFile.MultiSigAddresses.Values)
                if (address.Label == label)
                    return false;
            foreach (var address in x1WalletFile.ColdStakingAddresses.Values)
                if (address.Label == label)
                    return false;

            return true;
        }

        static int GetNextIndex(int isChange, X1WalletFile x1WalletFile, out int[] indexesInUse)
        {
            indexesInUse = GetIndexesInUse(isChange, x1WalletFile).ToArray();
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

        static List<int> GetIndexGaps(int isChange, X1WalletFile x1WalletFile)
        {
            List<int> numbers = new List<int>();
            var nextIndex = GetNextIndex(isChange, x1WalletFile, out int[] indexesInUse);
            for (var i = 0; i < nextIndex + C.GapLimit; i++)
                numbers.Add(i);
            for (var k = 0; k < indexesInUse.Length; k++)
                numbers.Remove(indexesInUse[k]);
            return numbers;
        }

        static IEnumerable<int> GetIndexesInUse(int isChange, X1WalletFile x1WalletFile)
        {
            return x1WalletFile.PubKeyHashAddresses.Values.Where(x =>
                x.KeyMaterial.KeyType == KeyType.Hd &&
                x.KeyMaterial.IsChange == isChange &&
                x.KeyMaterial.AddressIndex.HasValue).Select(x => x.KeyMaterial.AddressIndex.Value);
        }


        public static void TryUpdateLookAhead(string passphrase, X1WalletFile x1WalletFile)
        {
            if (string.IsNullOrWhiteSpace(passphrase))
                return;

            x1WalletFile.LookAhead.Clear();

            var decryptedSeed = VCL.DecryptWithPassphrase(passphrase, x1WalletFile.HdSeed);
            var changeAddressGaps = GetIndexGaps(C.Change, x1WalletFile);
            foreach (var index in changeAddressGaps)
            {
                var generated = GeneratePubKeyHashAddress(decryptedSeed, passphrase, x1WalletFile.CoinType, C.Change, index);
                x1WalletFile.LookAhead[generated.Address] = generated;
            }

            var receiveAddressGaps = GetIndexGaps(C.External, x1WalletFile);
            foreach (var index in receiveAddressGaps)
            {
                var generated = GeneratePubKeyHashAddress(decryptedSeed, passphrase, x1WalletFile.CoinType, C.Change, index);
                x1WalletFile.LookAhead[generated.Address] = generated;
            }

            x1WalletFile.SaveX1WalletFile(x1WalletFile.CurrentPath);
        }

        public static ISegWitAddress GetOrAddAddress(string bech32, int blockHeight, X1WalletFile x1WalletFile)
        {
            ISegWitAddress existing = null;
            if (x1WalletFile.PubKeyHashAddresses.TryGetValue(bech32, out var pubKeyHashAddress))
                existing = pubKeyHashAddress;
            else if (x1WalletFile.ColdStakingAddresses.TryGetValue(bech32, out var coldStakingAddress))
                existing = coldStakingAddress;
            else if (x1WalletFile.MultiSigAddresses.TryGetValue(bech32, out var multiSigAddress))
                existing = multiSigAddress;
            if (existing != null)
            {
                existing.LastSeenHeight = blockHeight;
                x1WalletFile.SaveX1WalletFile(x1WalletFile.CurrentPath);
                return existing;
            }

            if (x1WalletFile.LookAhead.TryGetValue(bech32, out var gapAddress))
            {
                gapAddress.LastSeenHeight = blockHeight;
                x1WalletFile.PubKeyHashAddresses[gapAddress.Address] = gapAddress;
                x1WalletFile.LookAhead.TryRemove(gapAddress.Address, out var _);
                x1WalletFile.SaveX1WalletFile(x1WalletFile.CurrentPath);
                return gapAddress;
            }
            return null;
        }

        public static ISegWitAddress FindAddress(string bech32, X1WalletFile x1WalletFile)
        {
            if (x1WalletFile.PubKeyHashAddresses.TryGetValue(bech32, out var pubKeyHashAddress))
                return pubKeyHashAddress;
            if (x1WalletFile.ColdStakingAddresses.TryGetValue(bech32, out var coldStakingAddress))
                return coldStakingAddress;
            if (x1WalletFile.MultiSigAddresses.TryGetValue(bech32, out var multiSigAddress))
                return multiSigAddress;
            return null;
        }

        static readonly Func<PubKeyHashAddress, bool> ChangeAddressesNeverSeenUsedOnTheChain = (x) => x.KeyMaterial.KeyType == KeyType.Hd &&   // must be hd
                                                                                      x.KeyMaterial.AddressIndex.HasValue &&   // if valid hd, this must have a value
                                                                                      x.KeyMaterial.IsChange == C.Change &&    // we require it's a change address 
                                                                                      x.LastSeenHeight == null;
        static readonly Func<PubKeyHashAddress, bool> AllPubKeyHashReceiveAddresses = (x) => x.AddressType == AddressType.PubKeyHash &&
                                                                                      x.KeyMaterial.IsChange != C.Change;

        internal static PubKeyHashAddress[] GetAllPubKeyHashReceiveAddresses(int skip, int? take, X1WalletFile x1WalletFile)
        {
            var store = x1WalletFile.PubKeyHashAddresses.Values;
            var filter = AllPubKeyHashReceiveAddresses;
            return take.HasValue
                ? store.Where(filter).Skip(skip).Take(take.Value).ToArray()
                : store.Where(filter).ToArray();
        }
    }
}
