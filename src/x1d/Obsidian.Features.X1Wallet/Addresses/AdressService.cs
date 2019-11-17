using System;
using System.Collections.Generic;
using System.Linq;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;

namespace Obsidian.Features.X1Wallet.Addresses
{
    static class AddressService
    {

        public static PubKeyHashAddress[] GetReceiveAddresses(int count, bool used, string passphrase, X1WalletFile x1WalletFile)
        {
            if (used)
            {
                return x1WalletFile.PubKeyHashAddresses.Values.Take(count).ToArray();
            }
            else
            {
                return x1WalletFile.CreateNewAddresses(C.External, passphrase, count).ToArray();
            }

        }

        public static PubKeyHashAddress[] GetPubKeyHashAddresses(int isChange, int? take, X1WalletFile x1WalletFile)
        {
            return take.HasValue
                ? x1WalletFile.PubKeyHashAddresses.Values.Where(x => x.KeyMaterial.IsChange == isChange).Take(take.Value).ToArray()
                : x1WalletFile.PubKeyHashAddresses.Values.Where(x => x.KeyMaterial.IsChange == isChange).ToArray();
        }



        /// <summary>
        /// Gets an unused receive address or throws en exception.
        /// </summary>
        public static PubKeyHashAddress GetUnusedReceiveAddress( X1WalletFile x1WalletFile)
        {
            return x1WalletFile.GetReceiveAddress(false, null);
        }

        public static PubKeyHashAddress GetUnusedChangeAddress(string passphrase, bool isDummy, X1WalletFile x1WalletFile)
        {
            return x1WalletFile.GetChangeAddress(passphrase, isDummy);
        }




        static void EnsureAddressBuffer(X1WalletFile x1WalletFile)
        {
            //this.GetUnusedReceiveAddress()
            //this.X1WalletFile.CreateNewAddresses();
        }




        static bool IsAddressUsedInConfirmedTransactions(ISegWitAddress address, IReadOnlyCollection<BlockMetadata> blocks)
        {
            // slow version
            foreach (BlockMetadata block in blocks)
            {
                foreach (TransactionMetadata tx in block.Transactions)
                {
                    foreach (var utxo in tx.Received.Values)
                    {
                        if (utxo.Address == address.Address)
                            return true;
                    }

                }
            }
            return false;
        }


    }
}
