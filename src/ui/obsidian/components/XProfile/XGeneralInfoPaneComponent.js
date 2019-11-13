import React from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { clean } from 'semver'
import { Box } from 'rebass'
import { Bar, CopyBox, DataRow, QRCode, Text } from 'components/UI'
import xmessages from './xmessages'

/**
 * backupMethodMessageMapper - Returns intl message for the specified provider.
 *
 * @param {string} provider Provider
 * @param {intlShape} intl Intl
 * @returns {string} Intl message
 */
function backupMethodMessageMapper(provider, intl) {
  const MAP = {
    gdrive: xmessages.backup_method_gdrive,
    dropbox: xmessages.backup_method_dropbox,
    local: xmessages.backup_method_local,
  }
  const intlMsg = MAP[provider]
  return intlMsg && intl.formatMessage({ ...intlMsg })
}

const XGeneralInfoPaneComponent = ({
  intl,
  fullNodeSettings,
  commitString,
  nodeUriOrPubkey,
  showNotification,
  backupProvider,
  versionString,
  ...rest
}) => {
  const notifyOfCopy = () =>
    showNotification(intl.formatMessage({ ...xmessages.pubkey_copied_notification_description }))

  return (
    <Box as="section" {...rest}>
      <Text fontWeight="normal">
        <FormattedMessage {...xmessages.generalinfo_pane_title} />
      </Text>
      <Bar mb={4} mt={2} />
      <DataRow
        left={
          <>
            <Text fontWeight="normal" mb={2}>
              Network
            </Text>
            <Text color="gray" fontWeight="light">
              The blockchain the node is running.
            </Text>
          </>
        }
        py={2}
        right={fullNodeSettings.network}
      />
      <DataRow
        left={
          <>
            <Text fontWeight="normal" mb={2}>
              Coin Ticker
            </Text>
            <Text color="gray" fontWeight="light">
              The ticker symbol that usually refers to one unit on this blockchain. 
            </Text>
          </>
        }
        py={2}
        right={fullNodeSettings.coinTicker}
      />
      <DataRow
        left={
          <>
            <Text fontWeight="normal" mb={2}>
              Chain Tip
            </Text>
            <Text color="gray" fontWeight="light">
              The tip of the best known validated chain from the Chain Indexer.
            </Text>
          </>
        }
        py={2}
        right={fullNodeSettings.chainTip}
      />
      <DataRow
        left={
          <>
            <Text fontWeight="normal" mb={2}>
              Chain Synced
            </Text>
            <Text color="gray" fontWeight="light">
              Whether the chain tip is synced with the network. Only when this is true, can the
              client calculate a download percentage based on Chain Tip and Last Block Synced
              Height.
            </Text>
          </>
        }
        py={2}
        right={fullNodeSettings.isChainSynced.toString()}
      />

      <DataRow
        left={
          <>
            <Text fontWeight="normal" mb={2}>
              Wallet Last Block Synced Height
            </Text>
            <Text color="gray" fontWeight="light">
              The highest block of the chain the wallet has already processed. If this equals the
              Chain Tip, the wallet has synced.
            </Text>
          </>
        }
        py={2}
        right={
          <>
            <Text>{fullNodeSettings.lastBlockSyncedHeight}</Text>
            <Text color="grey" fontSize="s">
              {fullNodeSettings.isChainSynced &&
              fullNodeSettings.chainTip &&
              fullNodeSettings.lastBlockSyncedHeight
                ? (
                    (fullNodeSettings.lastBlockSyncedHeight / fullNodeSettings.chainTip) *
                    100
                  ).toFixed(1) + ' %'
                : 'n/a'}
            </Text>
          </>
        }
      />

      <DataRow
        left={
          <>
            <Text fontWeight="normal" mb={2}>
              Connected Nodes
            </Text>
            <Text color="gray" fontWeight="light">
              The total number of nodes that we're connected to.
            </Text>
          </>
        }
        py={2}
        right={
          <>
            <Text>{fullNodeSettings.connectedNodes}</Text>
            <Text color="grey" fontSize="s">
              in/out
            </Text>
          </>
        }
      />
      <DataRow
        left={
          <>
            <Text fontWeight="normal">Wallet Creation Date</Text>
            <Text color="gray" fontWeight="light">
              The wallet will not search the blockchain for transactions before this date.
            </Text>
          </>
        }
        py={2}
        right={new Date(fullNodeSettings.creationTime * 1000).toUTCString()}
      />
      <DataRow
        left={
          <>
            <Text fontWeight="normal">Wallet Data File</Text>
            <Text color="gray" fontWeight="light">
              To backup, copy this file.
            </Text>
          </>
        }
        py={2}
        right={fullNodeSettings.walletFilePath}
      />
      <DataRow
        left={
          <>
            <Text fontWeight="normal">Decrypted</Text>
            <Text color="gray" fontWeight="light">
              This property is true if the wallet is unlocked for staking.
            </Text>
          </>
        }
        py={2}
        right={fullNodeSettings.isDecrypted.toString()}
      />
       <div>------------</div>
        <DataRow
          left={
            <>
              <Text fontWeight="normal">X1 Version</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={fullNodeSettings.agent}
        />

        <DataRow
          left={
            <>
              <Text fontWeight="normal">Node Version</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={'Stratis ' + fullNodeSettings.version}
        />

        <DataRow
          left={
            <>
              <Text fontWeight="normal">{productName} Version</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={version}
        />

        <DataRow
          left={
            <>
              <Text fontWeight="normal">VisuaCrypt Version</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={2}
        />

        <DataRow
          left={
            <>
              <Text fontWeight="normal">Protocol Version</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={fullNodeSettings.protocolVersion}
        />
        <Text fontWeight="normal" mt={4}>
          Node Features
        </Text>
        <Bar mb={4} mt={2} />
        {fullNodeSettings.featuresData &&
          fullNodeSettings.featuresData.map(w =>
            w.namespace === 'Stratis.Bitcoin.Base.BaseFeature' ? null : (
              <DataRow
                left={
                  <>
                    <Text fontWeight="normal">{w.namespace}</Text>
                    <Text color="gray" fontWeight="light"></Text>
                  </>
                }
                py={2}
                right={w.state}
              />
            )
          )}
        <Text fontWeight="normal" mt={4}>
          Node Process
        </Text>
        <Bar mb={4} mt={2} />
        <DataRow
          left={
            <>
              <Text fontWeight="normal">Process ID</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={fullNodeSettings.processId}
        />

        <DataRow
          left={
            <>
              <Text fontWeight="normal">Uptime</Text>
              <Text color="gray" fontWeight="light"></Text>
            </>
          }
          py={2}
          right={fullNodeSettings.runningTime}
        />
    </Box>
  )
}

XGeneralInfoPaneComponent.propTypes = {
  fullNodeSettings: PropTypes.object.isRequired,
}

export default injectIntl(XGeneralInfoPaneComponent)
