using System.Collections.Generic;
using Obsidian.Features.X1Wallet.Models.Wallet;

namespace Obsidian.Features.X1Wallet.Models.Api.Responses
{
    public sealed class GetAddressesResponse
    {
        public PubKeyHashAddress[] PubKeyHashAddresses { get; set; }
    }

    public sealed class GetAddressesRequest
    {
        public int Skip { get; set; }
        public int? Take { get; set; }
    }
}