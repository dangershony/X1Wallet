using System;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Wallet.Broadcasting;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Obsidian.Features.X1Wallet.Tests.Fakes
{
    sealed class TestBroadcastManager : IBroadcasterManager
    {
        public event EventHandler<TransactionBroadcastEntry> TransactionStateChanged;

        public void AddOrUpdate(Transaction transaction, State state, MempoolError mempoolError = null)
        {
            throw new NotImplementedException();
        }

        public Task BroadcastTransactionAsync(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public TransactionBroadcastEntry GetTransaction(uint256 transactionHash)
        {
            throw new NotImplementedException();
        }
    }
}
