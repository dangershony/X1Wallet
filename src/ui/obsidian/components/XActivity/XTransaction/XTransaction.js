import React from 'react'
import PropTypes from 'prop-types'
import { FormattedTime, FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { Box, Flex } from 'rebass'
import { Message, Text } from '../../../../renderer/components/UI'
import { CryptoValue, FiatValue } from '../../../../renderer/containers/UI'
import xmessages from './xmessages'

const XTransaction = ({ transaction, showXActivityModal, cryptoUnitName, intl }) => {
  const {
    txType,
    valueAdded,
    totalSpent,
    totalReceived,
    hashTx,
    heightBlock,
    timeBlock,
    hashBlock,
    recipients,
  } = transaction

  let info = ''
  if (recipients && recipients.length && recipients.length > 0) {
    if (recipients.length === 1) {
      info += '➙ ' + recipients[0].address
    } else {
      info += recipients.length + ' Recipients: ' + recipients[0].address + ', ...'
    }
  }
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
        data-hint={intl.formatMessage(xmessages.num_confirmations, {confirmations:transaction.confirmations, s: transaction.confirmations < 2 ? '' : 's'})}
        width={3 / 4}
      >
        <Text mb={1}>{<FormattedMessage {...xmessages[txType]} />}<span css={{ opacity: 0.5, position: 'relative', left: 30 }}> {info}</span></Text>
        
        {transaction.sending ? (
          <>
            {transaction.status === 'sending' && (
              <Message variant="processing">
                <FormattedMessage {...xmessages.status_processing} />
              </Message>
            )}
            {transaction.status === 'successful' && (
              <Message variant="success">
                <FormattedMessage {...xmessages.status_success} />
              </Message>
            )}
            {transaction.status === 'failed' && (
              <Message variant="error">
                <FormattedMessage {...xmessages.status_error} /> {transaction.error}
              </Message>
            )}
          </>
        ) : (
          <Text color="gray" fontSize="xs" fontWeight="normal">
            <span>{new Date(timeBlock * 1000).toTimeString().split(' ')[0]}</span>
            {/*  <FormattedTime value={transaction.time_stamp * 1000} hour='2-digit' minute='numeric' second='numeric' /> */}
          </Text>
        )}
      </Box>

      <Box
        className="hint--top-left"
        data-hint={intl.formatMessage({ ...xmessages.amount })}
        width={1 / 4}
      >
        <Box css={transaction.status == 'failed' ? { opacity: 0.2 } : null}>
          <Text color={valueAdded >= 0 ? 'superGreen' : null} mb={1} textAlign="right">
            {valueAdded >= 0 ? `+ ` : `- `}
            <CryptoValue value={valueAdded} />
            <i> {fullNodeCryptoUnitName}</i>
          </Text>
          <Text color="gray" fontSize="xs" fontWeight="normal" textAlign="right">
            <FiatValue style="currency" value={valueAdded} />
          </Text>
        </Box>
      </Box>
    </Flex>
  )
}

XTransaction.displayName = 'XTransaction'

XTransaction.propTypes = {
  cryptoUnitName: PropTypes.string.isRequired,
  intl: intlShape.isRequired,
  showXActivityModal: PropTypes.func.isRequired,
  transaction: PropTypes.object.isRequired,
}

export default injectIntl(XTransaction)
