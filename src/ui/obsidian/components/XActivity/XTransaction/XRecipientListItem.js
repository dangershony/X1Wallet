import React from 'react'
import { Box, Flex } from 'rebass'
import PropTypes from 'prop-types'
import XRecipientContainer from './XRecipientContainer'
import ChainLink from '../../../../renderer/components/Icon/ChainLink'
import Clock from '../../../../renderer/components/Icon/Clock'
import Zap from '../../../../renderer/components/Icon/Zap'
import { Text } from '../../../../renderer/components/UI'

const ZapIcon = () => <Zap height="1.6em" width="1.6em" />

const XActivityIcon = ({ recipient }) => {
  return <Clock />
  switch (activity.txType) {
    case 'Coinbase':
      return <ChainLink />
    case 'Coinstake':
      return <ChainLink />
    case 'Spend':
      return <ZapIcon />
    case 'WithinWallet':
      return <ZapIcon />
    case 'SpendWithoutChange':
      return <ZapIcon />
    case 'Receive':
      return <ZapIcon />
    default:
      return <Clock />
  }
}

XActivityIcon.propTypes = {
  recipient: PropTypes.object.isRequired,
}

const XRecipientListItem = ({ recipient, ...rest }) => {
  const ListItem = XRecipientContainer
  const listItemProps = { recipient: recipient }
  
  return (
    <Flex alignItems="center" justifyContent="space-between" {...rest}>
      <Text color="gray" mr={10} textAlign="center" width={24}>
        <XActivityIcon recipient={recipient} />
      </Text>
      <Box css={recipient && recipient.isSending ? null : { cursor: 'pointer' }} width={1}>
        <ListItem {...listItemProps} />
      </Box>
    </Flex>
  )
}

XRecipientListItem.propTypes = {
  recipient: PropTypes.object.isRequired,
}

export default React.memo(XRecipientListItem)
