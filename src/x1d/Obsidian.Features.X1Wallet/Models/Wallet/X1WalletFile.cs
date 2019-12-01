using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin.Utilities;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Models.Wallet
{
    public sealed class X1WalletFile
    {
        public const string FileExtension = ".x1wallet.json";

        public X1WalletFile()
        {
            this.LookAhead = new ConcurrentDictionary<string, PubKeyHashAddress>();
            this.PubKeyHashAddresses = new ConcurrentDictionary<string, PubKeyHashAddress>();
            this.MultiSigAddresses = new ConcurrentDictionary<string, MultiSigAddress>();
            this.ColdStakingAddresses = new ConcurrentDictionary<string, ColdStakingAddress>();
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

        public ConcurrentDictionary<string, PubKeyHashAddress> PubKeyHashAddresses { get; set; }

        public ConcurrentDictionary<string, ColdStakingAddress> ColdStakingAddresses { get; set; }

        public ConcurrentDictionary<string, MultiSigAddress> MultiSigAddresses { get; set; }

        public ConcurrentDictionary<string, PubKeyHashAddress> LookAhead { get; set; }

        [JsonIgnore]
        public string CurrentPath { get; set; }



    }
}
