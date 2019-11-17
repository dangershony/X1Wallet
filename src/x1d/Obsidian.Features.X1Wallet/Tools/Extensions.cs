using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;

namespace Obsidian.Features.X1Wallet.Tools
{
    public static class Extensions
    {
        /// <summary>
        /// // Pattern is: 1.0.*. The wildcard is: DateTime.Today.Subtract(new DateTime(2000, 1, 1)).Days;
        /// </summary>
        public static string GetShortVersionString(this Assembly assembly)
        {
            var version = assembly.GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        /// <summary>
        /// Checks is the block hash has a default value.
        /// </summary>
        /// <param name="hashBlock">block hash</param>
        /// <returns>true, if null, zero, or GenesisHash</returns>
        public static bool IsDefaultBlockHash(this uint256 hashBlock)
        {
            if (hashBlock == null || hashBlock == C.Network.GenesisHash || hashBlock == uint256.Zero)
                return true;
            return false;
        }

        public static void NotNull<K, T>(ref Dictionary<K, T> list, int capacity)
        {
            if (list == null)
                list = new Dictionary<K, T>(capacity);
        }

        /// <summary>
        /// Adds the item to the dictionary by overwriting the old value, and logging the occurence of the duplicate key as error. 
        /// </summary>
        public static void AddSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value, [CallerMemberName] string memberName = "", [CallerLineNumber] int line = 0)
        {
            if (dictionary.ContainsKey(key))
                Log.Logger.LogError($"The key '{key}' was already present in the dictionary. Method: {memberName}, Line: {line}");
            dictionary[key] = value;
        }

        public static Coin ToCoin(this SegWitCoin segWitCoin)
        {
            var outpoint = new OutPoint(segWitCoin.UtxoTxHash, segWitCoin.UtxoTxN);
            var txOut = new TxOut(segWitCoin.UtxoValue, segWitCoin.SegWitAddress.GetScriptPubKey());
            var coin = new Coin(outpoint, txOut);
            return coin;
        }

        public static bool HasCoinbaseMaturity(this TxType txType)
        {
            var type = (int)txType;
            return type > 0 && type < 30;
        }
    }
}
