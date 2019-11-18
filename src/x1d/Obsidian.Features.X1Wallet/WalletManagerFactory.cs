using System;
using System.Diagnostics;
using System.IO;
using NBitcoin;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet
{
    public sealed class WalletManagerFactory : IDisposable
    {
        readonly NodeServices nodeServices;
        readonly object lockObject = new object();

        WalletManager walletManager;

        public WalletManagerFactory(NodeServices nodeServices)
        {
            this.nodeServices = nodeServices;
        }

        public void Dispose()
        {
            using WalletContext context = AutoLoad(null, true);
            {
                context?.WalletManager?.Dispose();
            }
        }

        internal WalletContext AutoLoad(string walletName, bool doNotCheck = false)
        {
            if (doNotCheck)
            {
                if (this.walletManager == null)
                {
                    return null;
                }

                return new WalletContext(this.walletManager);
            }

            if (walletName == null) throw new ArgumentNullException(nameof(walletName));

            if (this.walletManager != null)
            {
                if (this.walletManager.WalletName == walletName) return new WalletContext(this.walletManager);

                throw new InvalidOperationException(
                    $"Invalid request for wallet {walletName} - the current wallet is {this.walletManager.WalletName}");
            }

            lock (this.lockObject)
            {
                if (this.walletManager == null)
                {
                    LoadWalletFromFile(walletName);
                    Debug.Assert(this.walletManager != null,
                        "The WalletSyncManager cannot be correctly initialized when the WalletManager is null");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Invalid request for wallet {walletName} - the current wallet is {this.walletManager.WalletName}");
                }
            }

            return new WalletContext(this.walletManager);
        }

        void LoadWalletFromFile(string walletName)
        {
            string x1WalletFilePath = walletName.GetX1WalletFilepath(C.Network, this.nodeServices.DataFolder);

            if (!File.Exists(x1WalletFilePath))
                throw new FileNotFoundException($"No wallet file found at {x1WalletFilePath}");

            if (this.walletManager != null)
            {
                if (this.walletManager.WalletPath != x1WalletFilePath)
                    throw new NotSupportedException(
                        "Core wallet manager already created, changing the wallet file while node and wallet are running is not currently supported.");
            }

            this.walletManager = new WalletManager(this.nodeServices, x1WalletFilePath);
        }

        public void CreateWallet(WalletCreateRequest walletCreateRequest)
        {
            string walletName = walletCreateRequest.WalletName;
            string filePath = walletName.GetX1WalletFilepath(C.Network, this.nodeServices.DataFolder);

            if (File.Exists(filePath))
                throw new InvalidOperationException(
                    $"A wallet with the name {walletName} already exists at {filePath}!");

            if (string.IsNullOrWhiteSpace(walletCreateRequest.Passphrase))
                throw new InvalidOperationException("A passphrase is required.");

            DateTime now = DateTime.UtcNow;

            var x1WalletFile = new X1WalletFile
            {
                WalletGuid = Guid.NewGuid(),
                WalletName = walletName,
                CoinTicker = C.Network.CoinTicker,
                CoinType = C.Network.Consensus.CoinType,
                CreatedUtc = now.ToUnixTime(),
                ModifiedUtc = now.ToUnixTime(),
                LastBackupUtc = null,
                Comment = "Your notes here!",
                Version = C.WalletKeyFileVersion,
                PassphraseChallenge = KeyHelper.GenerateRandomKeyMaterial(walletCreateRequest.Passphrase, 32)
                    .EncryptedPrivateKey
            };


            byte[] mnemonicBytes = KeyHelper.GetRandom(32);
            string bip39Passphrase = walletCreateRequest.Bip39Passphrase?.Trim() ?? "";

            Wordlist wl = Wordlist.English;
            var mnemonic = new Mnemonic(wl, mnemonicBytes);
            byte[] hdSeed = mnemonic.DeriveSeed(bip39Passphrase);

            x1WalletFile.HdSeed = VCL.EncryptWithPassphrase(walletCreateRequest.Passphrase, hdSeed);
            x1WalletFile.HdSeedHasBip39Passphrase = !string.IsNullOrWhiteSpace(bip39Passphrase);

            x1WalletFile.CreateNewAddresses(C.External, walletCreateRequest.Passphrase, C.GapLimit);
            x1WalletFile.CreateNewAddresses(C.Change, walletCreateRequest.Passphrase, C.GapLimit);

            x1WalletFile.SaveX1WalletFile(filePath);

            X1WalletMetadataFile x1WalletMetadataFile =
                x1WalletFile.CreateX1WalletMetadataFile(C.Network.GenesisHash);
            string x1WalletMetadataFilename = walletName.GetX1WalletMetaDataFilepath(C.Network, this.nodeServices.DataFolder);
            x1WalletMetadataFile.SaveX1WalletMetadataFile(x1WalletMetadataFilename);
        }
    }
}