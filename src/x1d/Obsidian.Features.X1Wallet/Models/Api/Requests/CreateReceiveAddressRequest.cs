using System;
using Obsidian.Features.X1Wallet.Models.Wallet;

namespace Obsidian.Features.X1Wallet.Models.Api.Requests
{
    public sealed class CreateReceiveAddressRequest
    {
        public string Label;
        public string Passphrase;
    }

    public sealed class CreateReceiveAddressResponse
    {
        public PubKeyHashAddress PubKeyHashAddress;
    }
}
