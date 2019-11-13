using System.Collections.Generic;

namespace Obsidian.Features.X1Wallet.Models.Api.Requests
{
    public class MultiSigTransactionRequest
    {
        public string Passphrase;
        public List<Recipient> Recipients;
        public List<Burn> Burns;
        public bool Sign;
        public bool Send;
        public string SourceMultiSigAddress;
    }
    
}