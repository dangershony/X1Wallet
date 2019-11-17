using Stratis.Bitcoin.Interfaces;

namespace Obsidian.Features.X1Wallet.Tests.Fakes
{
    sealed class TestInitialBlockDownloadState : IInitialBlockDownloadState
    {
        public bool IBD = true;

        public bool IsInitialBlockDownload()
        {
            return IBD;
        }
    }
}
