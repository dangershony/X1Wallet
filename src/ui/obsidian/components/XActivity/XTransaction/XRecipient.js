import React from 'react'
import PropTypes from 'prop-types'
import { FormattedTime, FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { Box, Flex } from 'rebass'
import { Message, Text } from '../../../../renderer/components/UI'
import { CryptoValue, FiatValue } from '../../../../renderer/containers/UI'
import xmessages from './xmessages'

const XRecipient = ({ recipient, showXActivityModal, cryptoUnitName, intl }) => {

if(!recipient)
return null

  const {
    address,
    amount,
  } = recipient

  
  const fullNodeCryptoUnitName = window.x1Tools.translateCryptoUnitName(cryptoUnitName)

  return (
    <Flex
      alignItems="center"
      justifyContent="space-between"
      onClick={() => showXActivityModal(transaction)}
      py={2}
    >
      <Box
        className="hint--top-right"
        data-hint={'a hint'}
        width={3 / 4}
      >
        <Text mb={1}>{address}</Text>
        
        
      </Box>

      <Box
        className="hint--top-left"
        data-hint={intl.formatMessage({ ...xmessages.amount })}
        width={1 / 4}
      >
        <Box css={address == 'failed' ? { opacity: 0.2 } : null}>
          <Text color={amount >= 0 ? 'superGreen' : null} mb={1} textAlign="right">
            {amount >= 0 ? `+ ` : `- `}
            <CryptoValue value={amount} />
            <i> {fullNodeCryptoUnitName}</i>
          </Text>
          <Text color="gray" fontSize="xs" fontWeight="normal" textAlign="right">
            <FiatValue style="currency" value={amount} />
          </Text>
        </Box>
      </Box>
    </Flex>
  )
}

XRecipient.displayName = 'XRecipient'

XRecipient.propTypes = {
  cryptoUnitName: PropTypes.string.isRequired,
  intl: intlShape.isRequired,
  showXActivityModal: PropTypes.func.isRequired,
  recipient: PropTypes.object.isRequired,
}

export default injectIntl(XRecipient)
