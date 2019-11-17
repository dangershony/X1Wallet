using System;
using System.Collections.Generic;
using System.Linq;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Staking;
using Obsidian.Features.X1Wallet.Tools;

namespace Obsidian.Features.X1Wallet.Balances
{
    static class BalanceService
    {
        public static Balance GetBalance(Dictionary<int, BlockMetadata> blocks, int syncedHeight, HashSet<MemoryPoolEntry> memoryPoolEntries, Func<string, ISegWitAddress> getOwnAddress,  
            string matchAddress = null, AddressType matchAddressType = AddressType.MatchAll)
        {
            var balance = new Balance();

            var spent = new Dictionary<string, UtxoMetadata>();

            // process all confirmed transactions first, oldest to newest
            foreach (var (height, block) in blocks)
            {
                foreach (var tx in block.Transactions)
                {
                    bool isImmatureForSpending = false;

                    if (tx.TxType.HasCoinbaseMaturity())
                    {
                        var confirmationsSpending = syncedHeight - height + 1; // if the tip is at 100 and my tx is height 90, it's 11 confirmations
                        isImmatureForSpending = confirmationsSpending < C.Network.Consensus.CoinbaseMaturity; // ok
                    }

                    var confirmationsStaking = syncedHeight - height + 1; // if the tip is at 100 and my tx is height 90, it's 11 confirmations
                    var isImmatureForStaking = confirmationsStaking < C.Network.Consensus.MaxReorgLength;

                    if (tx.Received != null)
                        foreach (UtxoMetadata utxo in tx.Received.Values)
                        {
                            for (var address = getOwnAddress(utxo.Address).Match(matchAddress, matchAddressType);
                                address != null; address = null)
                            {
                                balance.TotalReceived += utxo.Satoshis;

                                var coin = new SegWitCoin(address, utxo.HashTx, utxo.Index, utxo.Satoshis);

                                if (!isImmatureForSpending)
                                {
                                    balance.Spendable += utxo.Satoshis;
                                    balance.SpendableCoins.AddSafe(utxo.GetKey(), coin);
                                }
                                if (!isImmatureForStaking)
                                {
                                    balance.Stakable += utxo.Satoshis;
                                    balance.StakingCoins.AddSafe(utxo.GetKey(), coin);
                                }
                            }

                        }

                    if (tx.Spent != null)
                        foreach ((string txIdN, UtxoMetadata utxo) in tx.Spent)
                        {
                            balance.TotalSpent += utxo.Satoshis;
                            spent.AddSafe(txIdN, utxo);
                        }

                }
            }

            // unconfirmed transactions - add them last, ordered by time, so that they come last in coin selection
            // when unconfirmed outputs get spent, to allow the memory pool and network to recognize 
            // the new unspent outputs.
            foreach (MemoryPoolEntry entry in memoryPoolEntries.OrderBy(x => x.TransactionTime))
            {
                var tx = entry.Transaction;
                if (tx.Received != null)
                    foreach (UtxoMetadata utxo in tx.Received.Values)
                    {
                        ISegWitAddress address = getOwnAddress(utxo.Address);

                        balance.TotalReceivedPending += utxo.Satoshis;

                        var coin = new SegWitCoin(address, utxo.HashTx, utxo.Index, utxo.Satoshis);
                        balance.SpendableCoins.AddSafe(utxo.GetKey(), coin);
                    }

                if (tx.Spent != null)
                    foreach (var s in tx.Spent)
                    {
                        balance.TotalSpentPending += s.Value.Satoshis;
                        spent.AddSafe(s.Key, s.Value);
                    }
            }

            // remove what is already spent
            foreach (var utxoId in spent)
            {
                if (balance.SpendableCoins.ContainsKey(utxoId.Key))
                {
                    balance.Spendable -= utxoId.Value.Satoshis;
                    balance.SpendableCoins.Remove(utxoId.Key);
                }
                if (balance.StakingCoins.ContainsKey(utxoId.Key))
                {
                    balance.Stakable -= utxoId.Value.Satoshis;
                    balance.StakingCoins.Remove(utxoId.Key);
                }
            }

            // last balance updates
            balance.Confirmed = balance.TotalReceived - balance.TotalSpent;
            balance.Pending = balance.TotalReceivedPending - balance.TotalSpentPending;
            balance.Total = balance.Confirmed + balance.Pending;

            return balance;
        }
    }
}
