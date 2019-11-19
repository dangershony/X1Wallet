using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Obsidian.Features.X1Wallet;
using Obsidian.Features.X1Wallet.Models.Api.Requests;
using Obsidian.Features.X1Wallet.Tools;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.Miner.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Recipient = Obsidian.Features.X1Wallet.Models.Api.Requests.Recipient;

namespace Obsidian.x1d.Util
{
    public static class TestBench
    {
        static ILogger _logger;
        static FullNode _fullNode;
        static string _walletName = "new1";
        static string _passPhrase = "passwordpassword";

        static WalletController Controller
        {
            get
            {
                var controller = _fullNode.NodeService<WalletController>();
                controller.SetWalletName(_walletName);
                return controller;
            }
        }

        public static async void RunTestCodeAsync(FullNode fullNode)
        {
            var i = 0;
            try
            {
                _logger = fullNode.NodeService<ILoggerFactory>().CreateLogger(typeof(TestBench).FullName);
                _fullNode = fullNode;

                //TryCopyWalletForUpdate();
                await LoadOrCreateWalletAsync();


                //Controller.EnsureDummyMultiSig1Of2Address();

                //SpendFromMultiSig();

                //await StartMiningAsync();

                await Task.Delay(20000);

                //await SplitAsync();

                for (i = 0; i < 50; i++)
                {
                    //await Send(100 * Satoshi.Long, "odx1qjdldsm72vr4tmlecjfstr5clkk6pzux47f3e8x"); // buta
                    ///await Send(10 * Satoshi.Long, "odx1qvar8r29r8llzj53q5utmcewpju59263h38250ws33lp2q45lmalqg5lmdd"); // blacky #2
                    //await Send(100_000 * Satoshi.Long, "odx1q0p84t3whrflupcw5nrax2le7c2jpn67erpymnx"); // blacky stratis core wallet
                    //await Task.Delay(1000);
                }

                //await SplitAsync();
              await TryStakingAsync();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error at {i}:{e}");
            }

        }

        static void SpendFromMultiSig()
        {
            var request = new MultiSigTransactionRequest
            {
                Passphrase = _passPhrase,
                SourceMultiSigAddress = "odx1qvar8r29r8llzj53q5utmcewpju59263h38250ws33lp2q45lmalqg5lmdd",
                Recipients = new List<Recipient>
                {
                    new Recipient
                    {
                        Address = "odx1qpm3mfhpfyepugg629k4tgwllxjf285vwwd3f4h", // in my wallet
                        Amount = 2 * C.SatoshisPerCoin,

                    }
                },
                Sign = true,
                Send = true, 
            };
            var response = Controller.BuildMultiSigTransaction(request);
            ;
        }

        static async Task LoadOrCreateWalletAsync()
        {
            try
            {
                Controller.LoadWallet();
                _logger.LogInformation($"Loaded wallet '{_walletName}'.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e.Message);

                if (!e.Message.StartsWith("No wallet file found"))
                    throw;

                Controller.CreateWallet(new WalletCreateRequest
                { WalletName = _walletName, Passphrase = _passPhrase });

                _logger.LogInformation($"Created a new wallet named '{_walletName}'.");
                await Task.Delay(2000);
                await LoadOrCreateWalletAsync();
            }
        }

        static void TryCopyWalletForUpdate()
        {
            var currentWalletPath = _fullNode.NodeService<DataFolder>().WalletPath;
            var currentSegments = currentWalletPath.Split(Path.DirectorySeparatorChar);
            var oldSegments = new List<string>();
            bool found = false;
            foreach (var seg in currentSegments)
            {
                if (!found && (seg == ".obsidian" || seg == "Obsidian"))
                {
                    if (seg == ".obsidian")
                        oldSegments.Add(".stratisnode");
                    else
                        oldSegments.Add("StratisNode");
                    found = true;
                }
                else
                {
                    oldSegments.Add(seg);
                }
            }

            var oldWalletDirPath = string.Join(Path.DirectorySeparatorChar, oldSegments);
            var oldWalletPath = Path.Combine(oldWalletDirPath, "new1.ODX.x1wallet.json");
            var newWalletPath = Path.Combine(currentWalletPath, "new1.ODX.x1wallet.json");
            if (!File.Exists(newWalletPath))
                if (File.Exists(oldWalletPath))
                    File.Copy(oldWalletPath, newWalletPath);
        }

        static async Task Send(long satoshis, string address)
        {

            var recipients = new List<Recipient> { new Recipient { Amount = satoshis, Address = address } };
            var tx = Controller.BuildTransaction(new TransactionRequest
            { Recipients = recipients, Passphrase = _passPhrase, Sign = true, Send = true });
        }

        static async Task TryStakingAsync()
        {
            try
            {
                _logger.LogInformation("Starting staking...");
                Controller.StartStaking(new StartStakingRequest { Passphrase = _passPhrase });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }




        static async Task SplitAsync()
        {


            TransactionResponse model = Controller.BuildSplitTransaction(new TransactionRequest { Passphrase = _passPhrase, Sign = true, Send = true });
        }

        static async Task StartMiningAsync()
        {

            //var ibd = _fullNode.NodeService<IInitialBlockDownloadState>();
            //try
            //{
            //    Controller.LoadWallet();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //    if (!e.Message.StartsWith("No wallet file found"))
            //        throw;
            //    Controller.CreateWallet(new WalletCreateRequest
            //    { WalletName = _walletName, Passphrase = _passPhrase });
            //    Console.WriteLine($"Created a new wallet {_walletName} for mining.");

            //    await StartMiningAsync();

            //}
            // await Task.Delay(10000);
            var model = Controller.GetUsedReceiveAddresses(new Features.X1Wallet.Models.Api.Responses.GetAddressesRequest{Skip=0, Take = 1});
            var address = model.PubKeyHashAddresses[0].Address;

            var script = new ReserveScript { ReserveFullNodeScript = address.GetScriptPubKey() };
            _ = Task.Run(() =>
            {
                _logger.LogInformation("Starting Miner...");

                while (!_fullNode.NodeLifetime.ApplicationStopping.IsCancellationRequested)
                {
                    _fullNode.NodeService<IPowMining>().GenerateBlocks(script, 1, 1000 * 1000);
                    _logger.LogInformation("Mining...");
                }
            }, _fullNode.NodeLifetime.ApplicationStopping);
        }
    }
}
