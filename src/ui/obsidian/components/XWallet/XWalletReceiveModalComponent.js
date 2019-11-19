import React from 'react'
import { FormattedMessage, injectIntl } from 'react-intl'
import { Box, Flex } from 'rebass'
import {
  Bar,
  Header,
  QRCode,
  Card,
  CopyButton,
  Text,
} from 'components/UI'
import xmessages from './xmessages'

const sampleAddress = 'odx1q0693fqjqze4h7jy44vpmp8qtpk8v2rws0xa486'

const XWalletReceiveModalComponent = ({
  fullNodeConnection,
  fullNodeSettings,
  showNotification,
  intl,
  ...rest
}) => {
  const notifyOfCopy = () =>
    showNotification(intl.formatMessage({ ...xmessages.address_copied_notification_description }))

  let { unusedAddress, defaultReceiveAddress } = fullNodeSettings.walletInfo.walletDetails

  let ok = false
  let isUsed
  let receiveAddress
  if (unusedAddress && unusedAddress.length === sampleAddress.length) {
    receiveAddress = unusedAddress
    isUsed = false
    ok = true
  } else if (defaultReceiveAddress && defaultReceiveAddress.length === sampleAddress.length) {
    receiveAddress = defaultReceiveAddress
    isUsed = true
    ok = true
  } else {
    receiveAddress = 'n/a'
  }

  const CopyBox2 = ({ value, hint, onCopy, ...rest }) => {
    return (
      <Card bg="tertiaryColor" borderRadius={5} p={0} {...rest} width={1}>
        <Flex justifyContent="space-between">
          <Text fontSize="l" pt={2} mt={1} textAlign="center" width={1}>
            {value}
          </Text>
          <Box bg="primaryColor">
            <CopyButton hint={hint} onCopy={onCopy} p={3} value={value} />
          </Box>
        </Flex>
      </Card>
    )
  }

  return (
    <Box {...rest}>
      <Header
        subtitle={<span>{fullNodeConnection.daemonInfo.networkName}</span>}
        title={
          <>
            {fullNodeSettings.walletInfo.walletDetails.walletName}
            <span> - </span>
            <FormattedMessage
              {...xmessages.receive_subtitle}
              values={{ cryptoAddressName: fullNodeConnection.daemonInfo.coinTicker }}
            />
          </>
        }
      />
      <Bar mt={2} />

      {ok && (
        <Flex alignItems="center" flexDirection="column" mb={3} mt={4}>
          <QRCode size="xxlarge" value={receiveAddress} mb={3} />
          {isUsed ? (
            <span>This address has been used before.</span>
          ) : (
            <span>This is an unused address.</span>
          )}
        </Flex>
      )}

      <CopyBox2
        hint={intl.formatMessage({ ...xmessages.copy_address })}
        my={30}
        onCopy={notifyOfCopy}
        value={receiveAddress}
      />
    </Box>
  )
}

export default injectIntl(XWalletReceiveModalComponent)
