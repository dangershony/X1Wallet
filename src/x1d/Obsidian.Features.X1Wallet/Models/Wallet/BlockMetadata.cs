﻿using System.Collections.Generic;
using NBitcoin;

namespace Obsidian.Features.X1Wallet.Models.Wallet
{
    public class BlockMetadata
    {
        public BlockMetadata()
        {
            this.Transactions = new HashSet<TransactionMetadata>();
        }

        public uint256 HashBlock { get; set; }

        public HashSet<TransactionMetadata> Transactions { get; set; }
        public uint Time { get; set; }
    }
}