import React, { useState } from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { Box } from 'rebass'
import { Bar, DataRow, Button, CopyBox, Message, QRCode, Text } from 'components/UI'
import xmessages from './xmessages'
import getPackageDetails from '@zap/utils/getPackageDetails'

const XNodeInfoPaneComponent = ({
  intl,
  lndconnectQRCode,
  showNotification,
  fullNodeSettings,
  fullNodeConnection,
  ...rest
}) => {
  const [isObfuscated, setIsObfuscated] = useState(true)
  const toggleReveal = () => setIsObfuscated(!isObfuscated)
  const buttonMessage = isObfuscated ? 'lndconnect_reveal_button' : 'lndconnect_hide_button'
  const notifyOfCopy = () =>
    showNotification(
      intl.formatMessage({ ...xmessages.lndconnect_copied_notification_description })
    )
  const { productName, version } = getPackageDetails()
  const { walletInfo } = fullNodeSettings
  const { daemonInfo } = fullNodeConnection
  const { connectionInfo } = walletInfo
  return (
    <Box as="section" {...rest}>
      <Text fontWeight="normal">Connected to {daemonInfo.networkName}</Text>
      <>
        <Bar mb={4} mt={2} />

        <DataRow
          left={
            <>
              <Text fontWeight="normal">Best received peer header height</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                Hash
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="normal">{connectionInfo.bestPeerHeight} </Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                {connectionInfo.bestPeerHash}
              </Text>
            </>
          }
        />
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Consensus height</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                Hash
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="normal">{walletInfo.consensusTipHeight} </Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                {walletInfo.consensusTipHash}
              </Text>
            </>
          }
        />
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Block Store height</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                Hash
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="normal">{walletInfo.blockStoreHeight}</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                {walletInfo.blockStoreHash}
              </Text>
            </>
          }
        />
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Wallet height</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                Hash
              </Text>
            </>
          }
          py={2}
          right={
            <>
              <Text fontWeight="normal">{walletInfo.walletDetails.syncedHeight}</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                {walletInfo.walletDetails.syncedHash}
              </Text>
            </>
          }
        />
        <Text fontWeight="normal" mt={4}>
          Connections
        </Text>
        <Bar mb={2} mt={2} />
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Outbound connections</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={connectionInfo.outBound}
        />
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Inbound connections</Text>
              <Text color="gray" fontWeight="light" fontSize="s">
                TCP port 46660 must be open on your firewall and your computer
              </Text>
            </>
          }
          py={2}
          right={connectionInfo.inBound}
        />

        <Text fontWeight="normal" mt={4}>
          Peers
        </Text>
        <Bar mb={2} mt={2} />
        {connectionInfo.peers &&
          connectionInfo.peers.map(w =>
            !w ? null : (
              <DataRow
                key={w.remoteSocketEndpoint}
                left={
                  <>
                    <Text fontWeight="light">{w.remoteSocketEndpoint}</Text>
                    <Text color="gray" fontWeight="light">
                      {w.isInbound ? 'inbound' : 'outbound'}
                    </Text>
                  </>
                }
                py={2}
                right={
                  <>
                    <Text fontWeight="light" fontSize="m">
                      {w.version && w.version && w.version.split(':').length == 2
                        ? w.version.split(':')[0]
                        : w.version}
                    </Text>
                    <Text color="gray" fontWeight="light" fontSize="s">
                      {w.version && w.version && w.version.split(':').length == 2
                        ? w.version.split(':')[1]
                        : null}
                    </Text>
                  </>
                }
              />
            )
          )}
      </>
    </Box>
  )
}

XNodeInfoPaneComponent.propTypes = {
  intl: intlShape.isRequired,
  showNotification: PropTypes.func.isRequired,
}

export default injectIntl(XNodeInfoPaneComponent)
