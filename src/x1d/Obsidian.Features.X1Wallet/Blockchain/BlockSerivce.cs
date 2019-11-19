using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using static Obsidian.Features.X1Wallet.Tools.Extensions;

namespace Obsidian.Features.X1Wallet.Blockchain
{
    static class BlockService
    {
        public static BlockMetadata AnalyzeBlock(Block block, int blockHeight, ICollection<BlockMetadata> blockReader, Func<string,int, ISegWitAddress> addressReader)
        {
            var transactions = FindTransactions(block.Transactions, blockHeight, blockReader, addressReader);

            return transactions == null
                ? null
                : new BlockMetadata { HashBlock = block.GetHash(), Time = block.Header.Time, Transactions = transactions };
        }

        static HashSet<TransactionMetadata> FindTransactions(IEnumerable<Transaction> transactions, int blockHeight, ICollection<BlockMetadata> blockReader, Func<string,int, ISegWitAddress> addressReader)
        {
            HashSet<TransactionMetadata> found = null;

            foreach (Transaction transaction in transactions)
            {
                var foundTransaction = AnalyzeTransaction(transaction, blockHeight, blockReader, addressReader);
                if(foundTransaction == null)
                    continue;

                NotNull(ref found, 2);

                found.Add(foundTransaction);
            }

            return found;
        }

        public static TransactionMetadata AnalyzeTransaction(Transaction transaction, int blockHeight, ICollection<BlockMetadata> blockReader, Func<string,int, ISegWitAddress> addressReader)
        {
            var spent = ExtractOutgoingFunds(transaction, blockReader, out var amountSpent);
            var received = ExtractIncomingFunds(transaction, blockHeight, spent != null, addressReader, out var amountReceived, out var destinations);

            if (received == null && spent == null)
                return null;

            return Combine(spent, received, destinations, amountSpent, amountReceived, transaction);

        }



        static TransactionMetadata Combine(Dictionary<string, UtxoMetadata> spent,
            Dictionary<string, UtxoMetadata> received, Dictionary<string, UtxoMetadata> destinations, long amountSpent, long amountReceived,Transaction sourceTransaction)
        {
            return new TransactionMetadata
            {
                TxType = GetTxType(sourceTransaction, received, destinations, spent),
                HashTx = sourceTransaction.GetHash(),
                Received = received,
                Destinations = destinations,
                Spent = spent,
                ValueAdded = amountReceived - amountSpent
            };
        }

        static Dictionary<string, UtxoMetadata> ExtractOutgoingFunds(Transaction transaction, IEnumerable<BlockMetadata> blockReader, out long amountSpent)
        {
            if (transaction.IsCoinBase)
            {
                amountSpent = 0;
                return null;
            }

            List<OutPoint> prevOuts = GetPrevOuts(transaction);
            Dictionary<string, UtxoMetadata> spends = null;
            long sum = 0;

            foreach (var b in blockReader) // iterate ovr the large collection in outer loop (only once)
            {
                findOutPointInBlock:
                foreach (OutPoint prevOut in prevOuts)
                {
                    TransactionMetadata prevTx = b.Transactions.SingleOrDefault(x => x.HashTx == prevOut.Hash);
                    if (prevTx != null)  // prevOut tx id is in the wallet
                    {
                        var prevWalletUtxo = prevTx.Received.Values.SingleOrDefault(x => x.Index == prevOut.N);  // do we have the indexed output?
                        if (prevWalletUtxo != null)  // yes, it's a spend from this wallet
                        {
                            NotNull(ref spends, transaction.Inputs.Count); // ensure the return collection is initialized
                            spends.Add(prevWalletUtxo.GetKey(), prevWalletUtxo);  // add the spend
                            sum += prevWalletUtxo.Satoshis; // add amount

                            if (spends.Count == transaction.Inputs.Count) // we will find no more spends than inputs, quick exit
                            {
                                amountSpent = sum;
                                return spends;
                            }

                            prevOuts.Remove(prevOut); // do not search for this item any more
                            goto findOutPointInBlock; // we need a new enumerator for the shortened collection
                        }
                    }  // is the next prvOut also in this block? That's definitely possible!
                }
            }
            amountSpent = sum;
            return spends; // might be null or contain less then the tx inputs in edge cases, e.g. if an private key was removed from the wallet and no more items than the tx inputs.
        }

        static Dictionary<string, UtxoMetadata> ExtractIncomingFunds(Transaction transaction, int blockHeight, bool didSpend, Func<string, int, ISegWitAddress> addressReader, out long amountReceived, out Dictionary<string, UtxoMetadata> destinations)
        {
            Dictionary<string, UtxoMetadata> received = null;
            Dictionary<string, UtxoMetadata> notInWallet = null;
            long sum = 0;
            int index = 0;

            foreach (var output in transaction.Outputs)
            {
                ISegWitAddress ownAddress = null;

                if (!output.IsProtocolOutput(transaction))
                    ownAddress = addressReader(output.ScriptPubKey.GetAddressFromScriptPubKey(), blockHeight);

                if (ownAddress != null)
                {
                    NotNull(ref received, transaction.Outputs.Count);

                    var item = new UtxoMetadata
                    {
                        Address = ownAddress.Address,
                        HashTx = transaction.GetHash(),
                        Satoshis = output.Value.Satoshi,
                        Index = index
                    };
                    received.Add(item.GetKey(), item);
                    sum += item.Satoshis;
                }
                else
                {   // For protocol tx, we are not interested in the other outputs.
                    // If we spent, the save the destinations, because the wallet wants to show where we sent coins to.
                    // if we did not spent, we do not save the destinations, because they are the other parties change address
                    // and we only received coins.
                    if (!transaction.IsCoinBase && !transaction.IsCoinStake && didSpend)
                    {
                        NotNull(ref notInWallet, transaction.Outputs.Count);
                        var dest = new UtxoMetadata
                        {
                            Address = output.ScriptPubKey.GetAddressFromScriptPubKey(),
                            HashTx = transaction.GetHash(),
                            Satoshis = output.Value != null ? output.Value.Satoshi : 0,
                            Index = index
                        };
                        notInWallet.Add(dest.GetKey(), dest);
                    }

                }
                index++;
            }

            destinations = received != null
                ? notInWallet
                : null;

            amountReceived = sum;
            return received;
        }


        static List<OutPoint> GetPrevOuts(Transaction transaction)
        {
            var prevOuts = new List<OutPoint>(transaction.Inputs.Count);
            foreach (TxIn input in transaction.Inputs)
            {
                prevOuts.Add(input.PrevOut);
            }

            return prevOuts;
        }

        static TxType GetTxType(Transaction transaction, Dictionary<string, UtxoMetadata> received, Dictionary<string, UtxoMetadata> destinations, Dictionary<string, UtxoMetadata> spent)
        {
            if (transaction.IsCoinBase)
                return TxType.Coinbase;
            if (transaction.IsCoinStake)
            {
                if (transaction.Outputs.Count == 2)
                    return TxType.CoinstakeLegacy;
                if (transaction.Outputs.Count == 3)
                    return TxType.Coinstake;
            }

            bool didReceive = received != null && received.Count > 0;
            bool didSpend = spent != null && spent.Count > 0;
            bool hasDestinations = destinations != null && destinations.Count > 0;

            if (didReceive)
            {
                if (!didSpend)
                    return TxType.Receive;

                // if we are here we also spent something
                if (!hasDestinations)
                    return TxType.WithinWallet;
                return TxType.Spend;
            }

            if (didSpend)
                return TxType.SpendWithoutChange; // we spent with no change to out wallet

            // if we are here, we neither spent or received and that should never happen for transactions that affect the wallet.
            throw new ArgumentException(
                $"{nameof(GetTxType)} cant't determine {nameof(TxType)} for transaction {transaction.GetHash()}.");
        }
    }
}
