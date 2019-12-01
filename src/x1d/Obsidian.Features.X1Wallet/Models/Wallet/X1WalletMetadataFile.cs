using System;
using System.Collections.Concurrent;
using NBitcoin;

namespace Obsidian.Features.X1Wallet.Models.Wallet
{
    public sealed class X1WalletMetadataFile
    {
        public X1WalletMetadataFile()
        {
            this.Blocks = new ConcurrentDictionary<int, BlockMetadata>();
        }

        public const string FileExtension = ".x1wallet.metadata.json";

        public string X1WalletAssemblyVersion { get; set; }

        /// <summary>
        /// The WalletGuid correlates the X1WalletFile and the X1WalletMetadataFile.
        /// </summary>
        public Guid WalletGuid { get; set; }

        /// <summary>
        /// The height of the last block that was synced.
        /// </summary>
        public int SyncedHeight { get; set; }

        /// <summary>
        /// The hash of the last block that was synced.
        /// </summary>
        public uint256 SyncedHash { get; set; }

        public uint256 CheckpointHash { get; set; }

        public int CheckpointHeight { get; set; }

        public ConcurrentDictionary<int, BlockMetadata> Blocks { get; set; }

        public MemoryPoolMetadata MemoryPool { get; set; }

    }
}