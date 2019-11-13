import React, { useState } from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { Box } from 'rebass'
import { Bar, DataRow, Button, CopyBox, Message, QRCode, Text } from 'components/UI'
import xmessages from './xmessages'

const getBool = value => (value ? <div style={{color: 'transparent', textShadow: '0 0 0 #39e673'}}>✔</div> : '❌')
const getDate = value => new Date(value * 1000).toTimeString()
const XStakingInfoPaneComponent = ({
  intl,
  lndconnectQRCode,
  showNotification,
  fullNodeSettings,
  ...rest
}) => {
  const [isObfuscated, setIsObfuscated] = useState(true)
  const toggleReveal = () => setIsObfuscated(!isObfuscated)
  const buttonMessage = isObfuscated ? 'lndconnect_reveal_button' : 'lndconnect_hide_button'
  const notifyOfCopy = () =>
    showNotification(
      intl.formatMessage({ ...xmessages.lndconnect_copied_notification_description })
    )
  const stakingInfo = fullNodeSettings.walletInfo.walletDetails.stakingInfo

  let { enabled } = stakingInfo

  const posV3 = posV3 =>
    posV3 ? (
      <>
        <Text fontWeight="normal" mt={4}>
          PoS V3 Parameters
        </Text>
        <Bar mb={2} mt={2} />
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Block Time</Text>

              <Text color="gray" fontWeight="light">
                Unix time
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{getDate(posV3.currentBlockTime)}</Text>
              <Text color="gray" fontWeight="light">
                {posV3.currentBlockTime}
              </Text>
            </>
          }
        ></DataRow>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Stake Modifier V2</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{posV3.stakeModifierV2}</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
        ></DataRow>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Target</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{posV3.target}</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
        ></DataRow>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Target Difficulty</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{posV3.targetDifficulty}</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
        ></DataRow>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Search Interval</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                Block Interval
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{posV3.searchInterval}s</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                {posV3.blockInterval}s
              </Text>
            </>
          }
        ></DataRow>
      </>
    ) : null
  /** stakingStatus */
  const stakingStatus = stakingStatus =>
    stakingStatus ? (
      <>
        <Text fontWeight="normal" mt={4}>
        Staking Status
        </Text>
        <Bar mb={2} mt={2} />
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Started</Text>

              <Text color="gray" fontWeight="light">
                Unix time
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{getDate(stakingStatus.startedUtc)}</Text>
              <Text color="gray" fontWeight="light">
                {stakingStatus.startedUtc}
              </Text>
            </>
          }
        ></DataRow>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Blocks accepted</Text>
              <Text color="gray" fontWeight="light">Blocks not accepted</Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{stakingStatus.blocksAccepted}</Text>
              <Text color="gray" fontWeight="light">{stakingStatus.blocksNotAccepted}</Text>
            </>
          }
        ></DataRow>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Exceptions</Text>
              <Text color="gray" fontWeight="light">Last Exception</Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{stakingStatus.exceptions}</Text>
              <Text color="gray" fontWeight="light">{stakingStatus.lastException}</Text>
            </>
          }
        ></DataRow>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Compute time</Text>
              <Text color="gray" fontWeight="light">Wait time</Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{stakingStatus.computeTimeMs}ms</Text>
              <Text color="gray" fontWeight="light">{stakingStatus.waitMs}ms</Text>
            </>
          }
        ></DataRow>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Kernels found</Text>
              <Text color="gray" fontWeight="light">Unspent outputs
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{stakingStatus.kernelsFound}</Text>
              <Text color="gray" fontWeight="light">{stakingStatus.unspentOutputs}</Text>
            </>
          }
        ></DataRow>
         <DataRow
          left={
            <>
              <Text fontWeight="normal">Staking weight</Text>
              <Text color="gray" fontWeight="light">
              Network weight
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{stakingStatus.weight}</Text>
              <Text color="gray" fontWeight="light">{stakingStatus.networkWeight}
              </Text>
            </>
          }
        ></DataRow>
         <DataRow
          left={
            <>
              <Text fontWeight="normal">Weight %</Text>
              <Text color="gray" fontWeight="light">Expected time
              </Text>
              <Text color="gray" fontWeight="light">Actual time 
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{stakingStatus.weightPercent}%</Text>
              <Text color="gray" fontWeight="light">{stakingStatus.expectedTime}s
              </Text>
              <Text color="gray" fontWeight="light">{stakingStatus.actualTime}s
              </Text>
            </>
          }
        ></DataRow>
         <DataRow
          left={
            <>
              <Text fontWeight="normal">Immature coins</Text>
              <Text color="gray" fontWeight="light">
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="light">{stakingStatus.immature}</Text>
              <Text color="gray" fontWeight="light">
              </Text>
            </>
          }
        ></DataRow>
      </>
    ) : null
/** stakingStatus */
const lastStakedBlock = lastStakedBlock =>
lastStakedBlock ? (
  <>
    <Text fontWeight="normal" mt={4}>
    Last staked block
    </Text>
    <Bar mb={2} mt={2} />
    <DataRow
      left={
        <>
          <Text fontWeight="normal">Block time</Text>

          <Text color="gray" fontWeight="light">
            Unix time
          </Text>
        </>
      }
      py={2}
      right={
        <>
          <Text fontWeight="light">{getDate(lastStakedBlock.blockTime)}</Text>
          <Text color="gray" fontWeight="light">
            {lastStakedBlock.blockTime}
          </Text>
        </>
      }
    ></DataRow>
    <DataRow
      left={
        <>
          <Text fontWeight="normal">Block height</Text>
          <Text color="gray" fontWeight="light">Block hash</Text>
        </>
      }
      py={2}
      right={
        <>
          <Text fontWeight="light">{lastStakedBlock.height}</Text>
          <Text color="gray" fontWeight="light">{lastStakedBlock.hash}</Text>
        </>
      }
    ></DataRow>
    <DataRow
      left={
        <>
          <Text fontWeight="normal">Your address</Text>
          <Text color="gray" fontWeight="light">TxId</Text>
        </>
      }
      py={2}
      right={
        <>
          <Text fontWeight="light">{lastStakedBlock.kernelAddress}</Text>
          <Text color="gray" fontWeight="light">{lastStakedBlock.txId}</Text>
        </>
      }
    ></DataRow>
    <DataRow
      left={
        <>
          <Text fontWeight="normal">Total reward</Text>
          <Text color="gray" fontWeight="light">Weight used</Text>
          <Text color="gray" fontWeight="light">Total compute time</Text>
        </>
      }
      py={2}
      right={
        <>
          <Text fontWeight="light">{lastStakedBlock.totalReward}</Text>
          <Text color="gray" fontWeight="light">{lastStakedBlock.weightUsed}</Text>
          <Text color="gray" fontWeight="light">{lastStakedBlock.totalComputeTimeMs}</Text>
        </>
      }
    ></DataRow>
    <DataRow
      left={
        <>
          <Text fontWeight="normal">Block size</Text>
          <Text color="gray" fontWeight="light">Transactions included
          </Text>
        </>
      }
      py={2}
      right={
        <>
          <Text fontWeight="light">{lastStakedBlock.blockSize}</Text>
          <Text color="gray" fontWeight="light">{lastStakedBlock.transactions}</Text>
        </>
      }
    ></DataRow>
    
  </>
) : null
  //enabled = false
  return (
    <Box as="section" {...rest}>
      {/* <Text fontWeight="normal">
        <FormattedMessage {...xmessages.stakinginfo_pane_title} />
      </Text> */}
      <>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Staking Enabled</Text>
              {enabled ? (
                <Text color="gray" fontWeight="light">
                  Your wallet is unlocked for staking
                </Text>
              ) : (
                <Text color="gray" fontWeight="light">
                  Your wallet is not unlocked for staking - try 'Start staking' from the Staking
                  menu
                </Text>
              )}
            </>
          }
          py={2}
          right={getBool(enabled)}
        />
        {posV3(stakingInfo.posV3)}
        {stakingStatus(stakingInfo.stakingStatus)}
        {lastStakedBlock(stakingInfo.lastStakedBlock)}
        {enabled ? (
          <>
            <Bar mb={4} mt={2} />
            <Bar mb={4} mt={2} />
            <Bar mb={4} mt={2} />
            <Text fontWeight="normal" mt={4}>
              Peers
            </Text>
            <Bar mb={4} mt={2} />

            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Node Staking</Text>
                  <Text color="gray" fontWeight="light">
                    The node is currently staking and trying to find blocks that satisfy the
                    difficulty target.
                  </Text>
                </>
              }
              py={2}
              right={stakingInfo.stakingStatus.weight > 0 ? 'true' : 'false'}
            />
            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Staking Weight</Text>
                  <Text color="gray" fontWeight="light">
                    Staking weight of the node.
                  </Text>
                </>
              }
              py={2}
              right={
                stakingInfo.stakingStatus.weight
                  ? (stakingInfo.stakingStatus.weight / 1000000 / 100000000).toFixed(3) + ' M'
                  : 'n/a'
              }
            />

            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Network Staking Weight</Text>
                  <Text color="gray" fontWeight="light">
                    Estimation of the total staking weight of all nodes on the network.
                  </Text>
                </>
              }
              py={2}
              right={
                stakingInfo.stakingStatus.networkWeight
                  ? (stakingInfo.stakingStatus.networkWeight / 1000000 / 100000000).toFixed(3) +
                    ' M'
                  : 'n/a'
              }
            />
            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Expected Time</Text>
                  <Text color="gray" fontWeight="light">
                    Expected time of the node to find new block in seconds.
                  </Text>
                </>
              }
              py={2}
              right={stakingInfo.stakingStatus.expectedTime + ' s'}
            />
            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Immature</Text>
                  <Text color="gray" fontWeight="light">
                    The amount in the wallet which is not suitable for staking due to having
                    insufficient confirmations.
                  </Text>
                </>
              }
              py={2}
              right={
                stakingInfo.stakingStatus.immature
                  ? (stakingInfo.stakingStatus.immature / 1000000 / 100000000).toFixed(3) + ' M'
                  : 'n/a'
              }
            />

            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Current Block Size</Text>
                  <Text color="gray" fontWeight="light">
                    Size of the next block the node attempts to mine in bytes.
                  </Text>
                </>
              }
              py={2}
              right={stakingInfo.currentBlockSize}
            />
            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Current Block Transactions</Text>
                  <Text color="gray" fontWeight="light">
                    Number of transactions (excluding coinbase/coinstake) the node has included in
                    the block.
                  </Text>
                </>
              }
              py={2}
              right={stakingInfo.currentBlockTx ? fullNodeSettings.currentBlockTx - 1 : 0}
            />
            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Memory Pool Size</Text>
                  <Text color="gray" fontWeight="light">
                    Number of transactions currently waiting in the memory pool to be included in a
                    block.
                  </Text>
                </>
              }
              py={2}
              right={stakingInfo.pooledTx}
            />
            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Relay Fee</Text>
                  <Text color="gray" fontWeight="light">
                    The minimum fee this node will charge to include a transaction into a block.
                  </Text>
                </>
              }
              py={2}
              right={stakingInfo.relayFee}
            />
            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Difficulty</Text>
                  <Text color="gray" fontWeight="light">
                    The network difficulty for the next block. Valid blocks must have a hash below
                    this target.
                  </Text>
                </>
              }
              py={2}
              right={stakingInfo.difficulty}
            />

            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Search Interval</Text>
                  <Text color="gray" fontWeight="light">
                    Length of the last staking search interval in seconds.
                  </Text>
                </>
              }
              py={2}
              right={stakingInfo.searchInterval + ' s'}
            />
            <DataRow
              left={
                <>
                  <Text fontWeight="normal">Warnings</Text>
                  <Text color="gray" fontWeight="light">
                    Last recoverable error that occured in the staking kernel loop.
                  </Text>
                </>
              }
              py={2}
              right={
                stakingInfo.stakingStatus.lastException
                  ? stakingInfo.stakingStatus.lastException
                  : 'none'
              }
            />
          </>
        ) : null}
      </>
    </Box>
  )
}

XStakingInfoPaneComponent.propTypes = {
  intl: intlShape.isRequired,
  showNotification: PropTypes.func.isRequired,
}

export default injectIntl(XStakingInfoPaneComponent)
