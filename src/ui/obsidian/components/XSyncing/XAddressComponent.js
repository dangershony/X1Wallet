import React from 'react'
import PropTypes from 'prop-types'
import copy from 'copy-to-clipboard'
import { Flex } from 'rebass'
import { Button, QRCode, Spinner, Text } from 'components/UI'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import xmessages from './xmessages'

const XAddressComponent = ({ address, isAddressLoading, intl, showNotification, ...rest }) => {
  const copyToClipboard = data => {
    copy(data)
    const notifBody = intl.formatMessage({ ...xmessages.address_copied_notification_description })
    showNotification(notifBody)
  }

  const renderLoading = () => {
    return (
      <>
        <Spinner />
        <Text>
          <FormattedMessage {...xmessages.generating_address} />
        </Text>
      </>
    )
  }

  const renderAddress = () => (
    <>
      <Text my={3}>
        <FormattedMessage {...xmessages.fund_title} />
      </Text>
      <QRCode mx="auto" size="small" value={address} />
      <Text my={3}>{address}</Text>
      <Button mx="auto" onClick={() => copyToClipboard(address)} size="small">
        <FormattedMessage {...xmessages.copy_address} />
      </Button>
    </>
  )

  return (
    <Flex alignItems="center" flexDirection="column" justifyContent="center" {...rest}>
      {isAddressLoading && renderLoading()}
      {!isAddressLoading && address && renderAddress()}
    </Flex>
  )
}

XAddressComponent.propTypes = {
  address: PropTypes.string,
  intl: intlShape.isRequired,
  isAddressLoading: PropTypes.bool,
  showNotification: PropTypes.func.isRequired,
}

export default injectIntl(XAddressComponent)
