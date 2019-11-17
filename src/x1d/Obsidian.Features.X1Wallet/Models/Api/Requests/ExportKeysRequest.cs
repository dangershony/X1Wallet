using Newtonsoft.Json;

namespace Obsidian.Features.X1Wallet.Models.Api.Requests
{
    public class ExportKeysRequest
    {
        public string WalletPassphrase { get; set; }
        [JsonIgnore]
        public string WalletName { get; set; }
    }
}