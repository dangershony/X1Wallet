import React from 'react'
import PropTypes from 'prop-types'
import { Box } from 'rebass'
import { Button } from 'components/UI'
import { FormattedMessage } from 'react-intl'
import xmessages from './xmessages'

const XWalletButtonsComponent = ({ openModal }) => (
  <Box as="section">
    <Button mr={2} onClick={() => openModal('XPAY_MODAL')} width={145}>
      <FormattedMessage {...xmessages.pay} />
    </Button>
    <Button onClick={() => openModal('XRECEIVE_MODAL')} width={145}>
      <FormattedMessage {...xmessages.request} />
    </Button>
  </Box>
)

XWalletButtonsComponent.propTypes = {
  openModal: PropTypes.func.isRequired,
}

export default XWalletButtonsComponent
