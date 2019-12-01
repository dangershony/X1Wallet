using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.Crypto;
using Obsidian.Features.X1Wallet.Balances;
using Obsidian.Features.X1Wallet.Models.Wallet;
using Obsidian.Features.X1Wallet.Tools;
using Obsidian.Features.X1Wallet.Transactions;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Mining;
using VisualCrypt.VisualCryptLight;

namespace Obsidian.Features.X1Wallet.Staking
{
    sealed class StakingService
    {
        public readonly StakingStatus Status;
        public readonly PosV3 PosV3;
        public readonly StakedBlock LastStakedBlock;
        readonly Task stakingTask;
        readonly CancellationTokenSource cts;
        readonly WalletManager walletManager;
        readonly string passphrase;
        readonly NodeServices nodeServices;
        readonly Stopwatch stopwatch;

        public StakingService(WalletManager walletManager, string passphrase, NodeServices nodeServices)
        {
            this.walletManager = walletManager;
            this.passphrase = passphrase;
            this.nodeServices = nodeServices;

            this.cts = new CancellationTokenSource();
            this.stakingTask = new Task(StakingLoop, this.cts.Token);
            this.stopwatch = Stopwatch.StartNew();
            this.Status = new StakingStatus { StartedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            this.PosV3 = new PosV3 { SearchInterval = 64, BlockInterval = 4 * 64 };
            this.LastStakedBlock = new StakedBlock();
        }

        public void Start()
        {
            if (this.stakingTask.Status != TaskStatus.Running)
            {
                this.stakingTask.Start();
            }
        }

        public void Stop()
        {
            if (!this.cts.IsCancellationRequested)
            {
                this.cts.Cancel();
            }
        }

        void StakingLoop()
        {
            long previousBlockTime = GetCurrentBlockTime();

            while (!this.cts.IsCancellationRequested)
            {
                try

                {
                    this.PosV3.CurrentBlockTime = GetCurrentBlockTime();

                    if (this.PosV3.CurrentBlockTime > previousBlockTime)
                    {
                        previousBlockTime = this.PosV3.CurrentBlockTime;

                        this.stopwatch.Restart();

                        Stake();

                        this.stopwatch.Stop();
                    }
                    else
                    {
                        Task.Delay(1000).Wait();
                    }
                }
                catch (Exception e)
                {
                    HandleError(e);
                }
            }

            void HandleError(Exception e)
            {
                if (e is ConsensusRuleException ce)
                {
                    if (ce.ConsensusError == ConsensusErrors.BlockTimestampTooEarly)
                    {
                        this.Status.OutCompeted += 1;
                    }
                    else
                    {
                        this.Status.LastException = $"Consensus Error: {ce.ConsensusError.Message}";
                        this.Status.Exceptions++;
                        Log.Logger.LogWarning(this.Status.LastException);
                    }
                }
                else
                {
                    this.Status.LastException = e.Message.Replace(":", "-");
                    this.Status.Exceptions++;
                    Log.Logger.LogError(e.ToString());
                }

            }
        }

        void Stake()
        {
            this.stopwatch.Restart();
            BlockTemplate blockTemplate = GetBlockTemplate();

            this.PosV3.TargetDifficulty = blockTemplate.Block.Header.Bits.Difficulty;

            this.Status.NetworkWeight = GetNetworkWeight();
            this.Status.ExpectedTime = GetExpectedTime(this.Status.NetworkWeight, out this.Status.WeightPercent);

            this.PosV3.TargetAsBigInteger = blockTemplate.Block.Header.Bits.ToBigInteger(); // for calculation
            Debug.Assert(
                this.PosV3.TargetAsBigInteger.Equals(new Target(blockTemplate.Block.Header.Bits.ToCompact())
                    .ToBigInteger()), "Effect of ToCompact()");
            this.PosV3.Target = blockTemplate.Block.Header.Bits.ToUInt256(); // same for display only
            this.PosV3.StakeModifierV2 = GetStakeModifierV22();

            var coins = GetUnspentOutputs();
            var validKernels = FindValidKernels(coins);

            this.Status.KernelsFound = validKernels.Count;
            this.Status.ComputeTimeMs = this.stopwatch.ElapsedMilliseconds;

            if (validKernels.Count > 0)
                CreateNextBlock(blockTemplate, validKernels);
        }

        uint256 GetStakeModifierV22()
        {
            return this.nodeServices.StakeChain.Get(this.nodeServices.ConsensusManager.Tip.HashBlock).StakeModifierV2;
        }

        List<SegWitCoin> FindValidKernels(SegWitCoin[] coins)
        {
            var validKernels = new List<SegWitCoin>();
            foreach (var c in coins)
            {
                if (CheckStakeKernelHash(c))
                    validKernels.Add(c);
            }
            return validKernels;
        }

        bool CheckStakeKernelHash(SegWitCoin segWitCoin)
        {
            BigInteger value = BigInteger.ValueOf(segWitCoin.UtxoValue);
            BigInteger weightedTarget = this.PosV3.TargetAsBigInteger.Multiply(value);

            uint256 kernelHash;
            using (var ms = new MemoryStream())
            {
                var serializer = new BitcoinStream(ms, true);
                serializer.ReadWrite(this.PosV3.StakeModifierV2);

                if (C.Network.CreateTransaction() is PosTransaction)
                {
                    //serializer.ReadWrite(stakingCoin.Time); // be sure this is uint
                    var oldFunnyTime = this.nodeServices.CoinView.FetchCoins(new[] { segWitCoin.UtxoTxHash }).UnspentOutputs[0].Time;
                    serializer.ReadWrite(oldFunnyTime); // it should be block time, but due to a bug in ppcoin we need oldFunnyTime
                }

                serializer.ReadWrite(segWitCoin.UtxoTxHash);
                serializer.ReadWrite(segWitCoin.UtxoTxN);
                serializer.ReadWrite((uint)this.PosV3.CurrentBlockTime); // be sure this is uint
                kernelHash = Hashes.Hash256(ms.ToArray());
            }

            var hash = new BigInteger(1, kernelHash.ToBytes(false));

            return hash.CompareTo(weightedTarget) <= 0;
        }

        BlockTemplate GetBlockTemplate()
        {
            return this.nodeServices.BlockProvider.BuildPosBlock(this.nodeServices.ConsensusManager.Tip, new Script());
        }

        void CreateNextBlock(BlockTemplate blockTemplate, List<SegWitCoin> kernelCoins)
        {
            SegWitCoin kernelCoin = kernelCoins[0];
            foreach (var coin in kernelCoins)
                if (coin.UtxoValue < kernelCoin.UtxoValue)
                    kernelCoin = coin;

            var newBlockHeight = this.nodeServices.ConsensusManager.Tip.Height + 1;

            var totalReward = blockTemplate.TotalFee + this.nodeServices.PosCoinviewRule.GetProofOfStakeReward(newBlockHeight);
            blockTemplate.Block.Header.Time = (uint)this.PosV3.CurrentBlockTime;

            Transaction tx = CoinstakeTransactionService.CreateAndSignCoinstakeTransaction(kernelCoin, totalReward.Satoshi, (uint)this.PosV3.CurrentBlockTime, this.passphrase, out Key blockSignatureKey);

            if (tx is PosTransaction posTransaction)
                ((PosTransaction)blockTemplate.Block.Transactions[0]).Time = (uint)this.PosV3.CurrentBlockTime;

            blockTemplate.Block.Transactions.Insert(1, tx);

            this.nodeServices.BlockProvider.BlockModified(this.nodeServices.ConsensusManager.Tip, blockTemplate.Block);

            ECDSASignature signature = blockSignatureKey.Sign(blockTemplate.Block.GetHash());
            ((PosBlock)blockTemplate.Block).BlockSignature = new BlockSignature { Signature = signature.ToDER() };

            ChainedHeader chainedHeader;
            try
            {
                chainedHeader = this.nodeServices.ConsensusManager.BlockMinedAsync(blockTemplate.Block).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                this.Status.BlocksNotAccepted++;
                throw;
            }

            if (chainedHeader == null)
            {
                this.Status.BlocksNotAccepted += 1;
                return;
            }

            if (this.LastStakedBlock.BlockTime != 0)
            {
                this.Status.ActualTime = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - this.LastStakedBlock.BlockTime);
            }
            else
            {
                this.Status.ActualTime = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - this.Status.StartedUtc);
            }

            this.LastStakedBlock.Hash = chainedHeader.HashBlock;
            this.LastStakedBlock.Height = chainedHeader.Height;
            this.LastStakedBlock.BlockSize = chainedHeader.Block.BlockSize ?? -1;
            this.LastStakedBlock.TxId = chainedHeader.Block.Transactions[1].GetHash();
            this.LastStakedBlock.Transactions = chainedHeader.Block.Transactions.Count;
            this.LastStakedBlock.TotalReward = totalReward;
            this.LastStakedBlock.KernelAddress = kernelCoin.SegWitAddress.Address;
            this.LastStakedBlock.WeightUsed = kernelCoin.UtxoValue;
            this.LastStakedBlock.TotalComputeTimeMs = this.stopwatch.ElapsedMilliseconds;
            this.LastStakedBlock.BlockTime = this.PosV3.CurrentBlockTime;
            this.Status.BlocksAccepted += 1;

            Log.Logger.LogInformation(
                $"Congratulations, your staked a new block at height {newBlockHeight} and received a total reward of {totalReward} {C.Network.CoinTicker}.");
        }

        double GetNetworkWeight()
        {
            var result = this.PosV3.TargetDifficulty * 0x100000000;
            if (result > 0)
            {
                result /= this.PosV3.BlockInterval;
                result *= this.PosV3.SearchInterval;
                return result;
            }
            return 0;
        }

        int GetExpectedTime(double networkWeight, out double ownPercent)
        {
            if (this.Status.Weight <= 0)
            {
                ownPercent = 0;
                return int.MaxValue;
            }

            var ownWeight = (double)this.Status.Weight;

            var ownFraction = ownWeight / networkWeight;

            var expectedTimeSeconds = this.PosV3.BlockInterval / ownFraction;
            ownPercent = Math.Round(ownFraction * 100, 1);

            return (int)(this.PosV3.BlockInterval + expectedTimeSeconds);
        }

        SegWitCoin[] GetUnspentOutputs()
        {
            try
            {
                this.walletManager.WalletSemaphore.Wait();

                Balance balance = this.walletManager.GetBalance(matchAddress: null, AddressType.MatchAll);

                this.Status.UnspentOutputs = balance.StakingCoins.Count;
                this.Status.Weight = balance.Stakable;
                this.Status.Immature = balance.Total - balance.Stakable;

                return balance.StakingCoins.Values.ToArray();
            }
            finally
            {
                this.walletManager.WalletSemaphore.Release();
            }
        }

        long GetCurrentBlockTime()
        {
            long currentAdjustedTime = this.nodeServices.DateTimeProvider.GetAdjustedTimeAsUnixTimestamp();
            long blockTime = currentAdjustedTime - currentAdjustedTime % this.PosV3.SearchInterval;

            var blockTimeViaMask = currentAdjustedTime & ~PosConsensusOptions.StakeTimestampMask;
            Debug.Assert(blockTime == blockTimeViaMask);

            return blockTime;
        }
    }
}
