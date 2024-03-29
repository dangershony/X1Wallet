import React from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage, FormattedTime, injectIntl, intlShape } from 'react-intl'
import truncateNodePubkey from '@zap/utils/truncateNodePubkey'
import { Box, Flex } from 'rebass'
import { Message, Text } from 'components/UI'
import { CryptoValue, FiatValue } from 'containers/UI'
import messages from './messages'

/**
 * getDisplayNodeName - Given a payment object devise the most appropriate display name.
 *
 * @param  {object} payment Payment
 * @param {intlShape} intl react-intl module
 * @returns {string} Display name
 */
const getDisplayNodeName = (payment, intl) => {
  const { dest_node_alias, dest_node_pubkey } = payment
  if (dest_node_alias) {
    return dest_node_alias
  }
  if (dest_node_pubkey) {
    return truncateNodePubkey(dest_node_pubkey)
  }

  // If all else fails, return the string 'unknown'.
  return intl.formatMessage({ ...messages.unknown })
}

const Payment = ({ payment, showActivityModal, cryptoUnitName, intl }) => {
  return (
    <Flex
      alignItems="center"
      justifyContent="space-between"
      onClick={payment.isSending ? null : () => showActivityModal('PAYMENT', payment.payment_hash)}
      py={2}
    >
      <Box
        className="hint--top-right"
        data-hint={intl.formatMessage({ ...messages.type })}
        width={3 / 4}
      >
        <Text mb={1}>{getDisplayNodeName(payment, intl)}</Text>
        {payment.isSending ? (
          <>
            {payment.status === 'sending' && (
              <Message variant="processing">
                <FormattedMessage {...messages.status_processing} />
              </Message>
            )}
            {payment.status === 'successful' && (
              <Message variant="success">
                <FormattedMessage {...messages.status_success} />
              </Message>
            )}
            {payment.status === 'failed' && (
              <Message variant="error">
                <FormattedMessage {...messages.status_error} />
                {` `}
                {payment.error}
              </Message>
            )}
          </>
        ) : (
          <Text color="gray" fontSize="xs" fontWeight="normal">
            <FormattedTime value={payment.creation_date * 1000} />
          </Text>
        )}
      </Box>

      <Box
        className="hint--top-left"
        data-hint={intl.formatMessage({ ...messages.amount })}
        width={1 / 4}
      >
        <Box css={payment.status == 'failed' ? { opacity: 0.3 } : null}>
          <Text mb={1} textAlign="right">
            {'- '}
            <CryptoValue value={payment.value_sat} />
            <i> {cryptoUnitName}</i>
          </Text>
          <Text color="gray" fontSize="xs" fontWeight="normal" textAlign="right">
            <FiatValue style="currency" value={payment.value_sat} />
          </Text>
        </Box>
      </Box>
    </Flex>
  )
}

Payment.propTypes = {
  cryptoUnitName: PropTypes.string.isRequired,
  intl: intlShape.isRequired,
  payment: PropTypes.object.isRequired,
  showActivityModal: PropTypes.func.isRequired,
}

export default injectIntl(Payment)
