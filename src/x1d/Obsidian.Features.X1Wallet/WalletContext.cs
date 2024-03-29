﻿using System;

namespace Obsidian.Features.X1Wallet
{
    sealed  class WalletContext : IDisposable
    {
        public readonly WalletManager WalletManager;

        public WalletContext(WalletManager walletManager)
        {
            this.WalletManager = walletManager ?? throw new ArgumentNullException(nameof(walletManager));
            this.WalletManager.WalletSemaphore.Wait();
        }

        public void Dispose()
        {
            this.WalletManager.WalletSemaphore.Release();
        }
    }
}
