using System;
using System.Collections.Generic;
using System.Threading;
using NBitcoin;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Obsidian.Features.X1Wallet.Tests.Fakes
{
    sealed class TestBlockStore : IBlockStore
    {
        public readonly Dictionary<uint256, Block> Blocks = new Dictionary<uint256, Block>();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Block GetBlock(uint256 blockHash)
        {
            Guard.NotNull(blockHash, nameof(blockHash));

            if (this.Blocks.ContainsKey(blockHash))
                return this.Blocks[blockHash];
            return null;
        }

        public uint256 GetBlockIdByTransactionId(uint256 trxid)
        {
            throw new NotImplementedException();
        }

        public List<Block> GetBlocks(List<uint256> blockHashes)
        {
            throw new NotImplementedException();
        }

        public Transaction GetTransactionById(uint256 trxid)
        {
            throw new NotImplementedException();
        }

        public Transaction[] GetTransactionsByIds(uint256[] trxids, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }
    }
}
