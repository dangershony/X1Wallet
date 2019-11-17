using System.Collections.Generic;
using Newtonsoft.Json;
using Obsidian.Features.X1Wallet.Staking;

namespace Obsidian.Features.X1Wallet.Balances
{
    public sealed class Balance
    {
        public Balance()
        {
            this.SpendableCoins = new Dictionary<string, SegWitCoin>();
            this.StakingCoins = new Dictionary<string, SegWitCoin>();
        }

        /// <summary>
        /// Total = Confirmed + Pending.
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// Confirmed = TotalReceived - TotalSpent.
        /// </summary>
        public long Confirmed { get; set; }


        /// <summary>
        /// Pending = TotalReceivedPending - TotalSpentPending.
        /// </summary>
        public long Pending { get; set; }

        /// <summary>
        /// The amount that has enough confirmations to be already spendable.
        /// </summary>
        public long Spendable { get; set; }

        /// <summary>
        /// The amount that has enough confirmations for staking.
        /// </summary>
        public long Stakable { get; set; }

        /// <summary>
        /// Spendable outputs with the sum od Spendable.
        /// Key: HashTx-N.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, SegWitCoin> SpendableCoins { get;  set; }

        /// <summary>
        /// Staking outputs with the sum od Stakable.
        /// Key: HashTx-N.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, SegWitCoin> StakingCoins { get; set; }


        internal long TotalReceived { get; set; }
        internal long TotalSpent { get; set; }
        internal long TotalReceivedPending { get; set; }
        internal long TotalSpentPending { get; set; }
    }
}
