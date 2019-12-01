using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using Obsidian.Features.X1Wallet.Feature;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Models.Api.Responses;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Addresses
{
    static class ImportExportService
    {
        public static ImportKeysResponse ImportKeys(ImportKeysRequest importKeysRequest, byte[] passphraseChallenge)
        {
            if (importKeysRequest == null)
                throw new ArgumentNullException(nameof(importKeysRequest));
            if (importKeysRequest.WalletPassphrase == null)
                throw new ArgumentNullException(nameof(importKeysRequest.WalletPassphrase));
            if (importKeysRequest.Keys == null)
                throw new ArgumentNullException(nameof(importKeysRequest.Keys));

            var delimiters = new HashSet<char>();
            foreach (var c in importKeysRequest.Keys.Trim().ToCharArray())
            {
                if (char.IsWhiteSpace(c))
                    delimiters.Add(c);
            }

            var items = importKeysRequest.Keys.Split(delimiters.ToArray());
            var possibleKeys = items.Where(i => i.Length == 52).Distinct().ToList();
            if (possibleKeys.Count == 0)
                throw new X1WalletException(HttpStatusCode.BadRequest, "Input material cointained no keys.");

            var test = VCL.DecryptWithPassphrase(importKeysRequest.WalletPassphrase, passphraseChallenge);
            if (test == null)
                throw new X1WalletException(HttpStatusCode.Unauthorized,
                    "Your passphrase is incorrect.");
            var importedAddresses = new List<string>();

            var obsidianNetwork = new ObsidianNetwork();

            foreach (var candidate in possibleKeys)
            {
                try
                {
                    var secret = new BitcoinSecret(candidate, obsidianNetwork);
                    var privateKey = secret.PrivateKey.ToBytes();
                    throw new NotImplementedException();
                    //var address = AddressHelper.CreateWithPrivateKey(privateKey, importKeysRequest.WalletPassphrase, AddressType.SingleKey);

                    //this.X1WalletFile.Addresses.Add(address.Address, address);
                    //importedAddresses.Add($"{secret.GetAddress()} -> {address.Address}");
                }
                catch (Exception e)
                {
                    Log.Logger.LogWarning($"Could not import '{candidate}' as key or address. {e.Message}");
                }

            }

            // this.X1WalletFile.SaveX1WalletFile(this.CurrentX1WalletFilePath);

            var response = new ImportKeysResponse
            { ImportedAddresses = importedAddresses, Message = $"Imported {importedAddresses.Count} addresses." };
            return response;
        }


        internal static ExportKeysResponse ExportKeys(ExportKeysRequest exportKeysRequest, IReadOnlyCollection<ISegWitAddress> addresses)
        {
            var header = new StringBuilder();
            header.AppendLine($"Starting export from wallet {exportKeysRequest.WalletName}, network {C.Network.Name} on {DateTime.UtcNow} UTC.");
            var errors = new StringBuilder();
            errors.AppendLine("Errors");
            var success = new StringBuilder();
            success.AppendLine("Exported Private Key (Hex); Unix Time UTC; IsChange; Address; Label:");
            int errorCount = 0;
            int successCount = 0;
            try
            {
                header.AppendLine($"{addresses.Count} found in wallet.");

                var enc = new Bech32Encoder($"{C.Network.CoinTicker.ToLowerInvariant()}key");

                foreach (var a in addresses)
                {
                    try
                    {
                        var decryptedKey = VCL.DecryptWithPassphrase(exportKeysRequest.WalletPassphrase, a.GetEncryptedPrivateKey());
                        if (decryptedKey == null)
                        {
                            errorCount++;
                            header.AppendLine(
                                $"Address '{a.Address}'  could not be decrpted with this passphrase.");
                        }
                        else
                        {
                            var privateKey = enc.Encode(0, decryptedKey);
                            success.AppendLine($"{privateKey};{a.Address}");
                            successCount++;
                        }
                    }
                    catch (Exception e)
                    {
                        header.AppendLine($"Exception processing Address '{a.Address}': {e.Message}");
                    }
                }

                header.AppendLine($"{errorCount} errors occured.");
                header.AppendLine($"{successCount} addresses with private keys successfully exported.");
            }
            catch (Exception e)
            {
                errors.AppendLine(e.Message);
                return new ExportKeysResponse { Message = $"Export failed because an exception occured: {e.Message}" };
            }

            return new ExportKeysResponse
            { Message = $"{header}{Environment.NewLine}{success}{Environment.NewLine}{errors}{Environment.NewLine}" };
        }

    }
}
