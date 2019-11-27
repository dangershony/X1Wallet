using System;
using System.Diagnostics;
using System.IO;
using NBitcoin;
using Obsidian.Features.X1Wallet.Addresses;
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
        bool isDisposing;

        public WalletManagerFactory(NodeServices nodeServices)
        {
            this.nodeServices = nodeServices;
        }

        public void Dispose()
        {
            this.isDisposing = true;
            using WalletContext context = AutoLoad(null, true);
            {
                context?.WalletManager?.Dispose();
            }
        }

        internal WalletManager AutoLoad2(string walletName, bool doNotCheck = false)
        {

            if (doNotCheck)
            {
                if (this.walletManager == null)
                {
                    return null;
                }

                return this.walletManager;
            }

            if (walletName == null) 
                throw new ArgumentNullException(nameof(walletName));

            if (this.isDisposing)
                throw new ObjectDisposedException($"{nameof(WalletManagerFactory)} has already been disposed.");

            if (this.walletManager != null)
            {
                if (this.walletManager.WalletName == walletName)
                    return this.walletManager;

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

            return this.walletManager;
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
            string passphrase = walletCreateRequest.Passphrase;

            if (File.Exists(filePath))
                throw new InvalidOperationException(
                    $"A wallet with the name {walletName} already exists at {filePath}!");

            if (string.IsNullOrWhiteSpace(passphrase))
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
                PassphraseChallenge = KeyHelper.GenerateRandomKeyMaterial(passphrase, 32)
                    .EncryptedPrivateKey,
                CurrentPath = filePath
            };


            byte[] mnemonicBytes = KeyHelper.GetRandom(32);
            string bip39Passphrase = walletCreateRequest.Bip39Passphrase?.Trim() ?? "";

            Wordlist wl = Wordlist.English;
            var mnemonic = new Mnemonic(wl, mnemonicBytes);
            byte[] hdSeed = mnemonic.DeriveSeed(bip39Passphrase);

            x1WalletFile.HdSeed = VCL.EncryptWithPassphrase(passphrase, hdSeed);
            x1WalletFile.HdSeedHasBip39Passphrase = !string.IsNullOrWhiteSpace(bip39Passphrase);

            // Create one receive addresses, so that GetUsedReceiveAddresses returns at least one address, even if it is not used in this case.
            AddressService.CreateAndInsertNewReceiveAddress("Default address", passphrase, x1WalletFile);

            AddressService.CreateAndInsertNewChangeAddresses(passphrase, C.UnusedChangeAddressBuffer, x1WalletFile);

            ColdStakingAddressService.EnsureDefaultColdStakingAddress(passphrase, x1WalletFile);
            MultiSigAddressService.EnsureDefaultMultisigAddress(passphrase, x1WalletFile);


            AddressService.TryUpdateLookAhead(passphrase, x1WalletFile);

            x1WalletFile.SaveX1WalletFile(filePath);

            X1WalletMetadataFile x1WalletMetadataFile =
                x1WalletFile.CreateX1WalletMetadataFile(C.Network.GenesisHash);
            string x1WalletMetadataFilename = walletName.GetX1WalletMetaDataFilepath(C.Network, this.nodeServices.DataFolder);
            x1WalletMetadataFile.SaveX1WalletMetadataFile(x1WalletMetadataFilename);
        }
    }
}