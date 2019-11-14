using NBitcoin;

namespace Obsidian.Features.X1Wallet.Tools
{
    /// <summary>
    /// Constants & quick access.
    /// </summary>
    public static class C
    {
        public static Network Network;

        /// <summary>
        /// The wallet key file version the code requires.
        /// When loading the file, and it has a lower version,
        /// it should be converted to this version.
        /// If the file has a higher version an error should
        /// be raised.
        /// </summary>
        public const int WalletKeyFileVersion = 2;


        /// <summary>
        /// Key path for external addresses. 
        /// </summary>
        public const int External = 0;

        /// <summary>
        /// Key path for internal addresses. 
        /// </summary>
        public const int Change = 1;

        /// <summary>
        /// How much unused addresses the wallet should ahead.
        /// </summary>
        public const int GapLimit = 2;

        /// <summary>
        /// A coin has 100_000_000 Satoshis.
        /// </summary>
        public const long SatoshisPerCoin = 100_000_000;
        

        /// <summary>
        /// Length of a bech32 PubKeyHash address.
        /// </summary>
        public static int PubKeyHashAddressLength => KeyHelper.GetRandom(20).ToPubKeyHashAddress().Length;

        /// <summary>
        /// Length of a bech32 Script address.
        /// </summary>
        public static int ScriptAddressLength => KeyHelper.GetRandom(32).ToScriptAddress().Length;
    }
}
