import React from 'react'
import PropTypes from 'prop-types'
import { Flex } from 'rebass'
import styled from 'styled-components'
import SettingsMenu from '../../../renderer/containers/Settings/SettingsMenu'
import XStakingMenuContainer from '../XStaking/XStakingMenuContainer'
import { Text } from '../../../renderer/components/UI/'
import { FormattedMessage } from 'react-intl'
import xmessages from './xmessages'

const StyledText = styled(Text)`
  cursor: pointer;
  transition: all 0.25s;
  &:hover {
    opacity: 0.6;
  }
`

const MenuItem = ({ children, ...rest }) => (
  <StyledText ml={4} pl={2} {...rest}>
    {children}
  </StyledText>
)

MenuItem.propTypes = {
  children: PropTypes.node.isRequired,
}

const AutopayMenuItem = ({ openModal }) => (
  <MenuItem onClick={() => openModal('AUTOPAY')}>
    <FormattedMessage {...xmessages.menu_item_autopay} />
  </MenuItem>
)

AutopayMenuItem.propTypes = {
  openModal: PropTypes.func.isRequired,
}

const XWalletMenuComponent = ({ openModal }) => {
  return (
    <Flex as="section">
      <XStakingMenuContainer />
      <SettingsMenu
        css={`
          position: relative;
          z-index: 40;
        `}
        ml={4}
      />
    </Flex>
  )
}

XWalletMenuComponent.propTypes = {
  openModal: PropTypes.func.isRequired,
}

export default XWalletMenuComponent
