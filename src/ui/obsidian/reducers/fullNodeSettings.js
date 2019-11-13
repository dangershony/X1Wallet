import { X1_FNC_RESET, X1_RECEIVE_WALLETINFO } from './fullNodeConnectionActions'

const initialState = {
  walletInfo: {
    consensusTipHeight: 0,
    consensusTipHash: '',
    consensusTipAge: 0,
    blockStoreHeight: 0,
    blockStoreHash: '',
    maxTipAge: 0,
    isAtBestChainTip: false,
    connectionInfo: {
      bestPeerHeight: 0,
      inBound: 0,
      outBound: 0,
      peers: [
        /*{
          version: '',
          remoteSocketEndpoint: '',
          bestReceivedTipHeight: 0,
          isInbound: false,
        },*/
      ],
    },
    assemblyName: '',
    assemblyVersion: '',
    walletDetails: {
      walletName: '',
      walletFilePath: '',
      syncedHeight: 0,
      syncedHash: '',
      balance: {
        stakable: 0,
        confirmed: 0,
        pending: 0,
        spendable: 0,
      },
      memoryPool: {
        entries: [],
      },
      adresses: 0,
      defaultAddress: '',
      unusedAddress: '',
      stakingInfo: {
        enabled: false,
        posV3: {
          currentBlockTime: 0,
          searchInterval: 0,
          blockInterval: 0,
          stakeModifierV2: '',
          target: '',
          targetDifficulty: 0,
        },
        stakingStatus: {
          startedUtc: 0,
          blocksAccepted: 0,
          blocksNotAccepted: 0,
          exceptions: 0,
          lastException: '',
          waitMs: 0,
          computeTimeMs: 0,
          kernelsFound: 0,
          unspentOutputs: 0,
          immature: 0,
          weight: 0,
          networkWeight: 0,
          weightPercent: 0,
          expectedTime: 0,
          actualTime: 0,
        },
        lastStakedBlock: {
          height: 0,
          blockTime: 0,
          totalReward: 0,
          blockSize: 0,
          transactions: 0,
          weightUsed: 0,
          totalComputeTimeMs: 0,
        },
      },
      passphraseChallenge: '',
    },
  },
}

export default function fullNodeSettings(state = initialState, action) {
  switch (action.type) {
    case X1_FNC_RESET:
      return Object.assign({}, initialState)

    case X1_RECEIVE_WALLETINFO:
      if (!action.walletInfo) {
        action.walletInfo = initialState.walletInfo
      } else if (!action.walletInfo.walletDetails) {
        action.walletInfo.walletDetails = initialState.walletInfo.walletDetails
      } else if (!action.walletInfo.walletDetails.stakingInfo.stakingStatus) {
        action.walletInfo.walletDetails.stakingInfo.stakingStatus =
          initialState.walletInfo.walletDetails.stakingInfo.stakingStatus
      }else if (!action.walletInfo.walletDetails.stakingInfo.posV3) {
        action.walletInfo.walletDetails.stakingInfo.posV3 =
          initialState.walletInfo.walletDetails.stakingInfo.posV3
      }else if (!action.walletInfo.walletDetails.stakingInfo.lastStakedBlock) {
        action.walletInfo.walletDetails.stakingInfo.lastStakedBlock =
          initialState.walletInfo.walletDetails.stakingInfo.lastStakedBlock
      }

      return Object.assign({}, state, {
        walletInfo: action.walletInfo,
      })

    default:
      return state
  }
}
