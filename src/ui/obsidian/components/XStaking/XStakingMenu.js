import React from 'react'
import PropTypes from 'prop-types'
import { Dropmenu, StatusIndicator } from '../../../renderer/components/UI'
import Circle from '../../../renderer/components/Icon/Circle'
import { Flex } from 'rebass'
import { FormattedMessage } from 'react-intl'
import XStakingMenuHeader from './XStakingMenuHeader'
import XStakingMenuSummary from './XStakingMenuSummary'
import xmessages from './xmessages'
import { PANE_NODEINFO, PANE_STAKINGINFO, PANE_GENERALINFO, PANE_IMPORTKEYS, PANE_EXPORTKEYS, PANE_RESCAN, DEFAULT_PANE } from './../XProfile/xconstants'

const XStakingMenu = ({
  openModal,
  lightningBalance,
  channelCount,
  pendingBalance,
  onchainBalance,
  cryptoUnitName,
  callStopStaking,
  enabled,
  staking,

  setIsPassphraseDialogOpen,
  setCloseAction,
  callUnlockWallet,
  ...rest
}) => {
  const onPassphraseDialogClosed = () => {
    callUnlockWallet()
  }

  const menuOptions = {}
  const items = [
    {
      type: 'content',
      id: 'header',
      content: (
        <XStakingMenuHeader
          channelCount={channelCount}
          lightningBalance={lightningBalance}
          onchainBalance={onchainBalance}
          p={2}
          pendingBalance={pendingBalance}
        />
      ),
    },

    {
      type: 'content',
      id: 'summary',
      content: (
        <XStakingMenuSummary
          cryptoUnitName={cryptoUnitName}
          lightningBalance={lightningBalance}
          onchainBalance={onchainBalance}
          pb={3}
          pendingBalance={pendingBalance}
          px={2}
        />
      ),
    },
    { id: 'bar1', type: 'bar' },
    {
      id: 'manage',
      title: <FormattedMessage {...xmessages.menu_item_channels_title} />,
      description: <FormattedMessage {...xmessages.menu_item_channels_description} />,
      onClick: () => openModal('XPROFILE_MODAL',{ pane: PANE_STAKINGINFO}),
    },
    { id: 'bar2', type: 'bar' },
    !enabled // staking enabled
      ? {
          id: 'create',
          title: <FormattedMessage {...xmessages.menu_item_channel_create_title} />,
          description: <FormattedMessage {...xmessages.menu_item_channel_create_description} />,
          onClick: () => {
            menuOptions.setIsOpen(false)
            setIsPassphraseDialogOpen(true)
            setCloseAction(onPassphraseDialogClosed)
          },
        }
      : {
          id: 'stopStaking',
          title: <FormattedMessage {...xmessages.menu_item_stop_staking_title} />,
          description: <FormattedMessage {...xmessages.menu_item_stop_staking_description} />,
          onClick: e => {
            menuOptions.setIsOpen(false)
            callStopStaking()
          },
        },
  ]

  return (
    <Dropmenu items={items} menuOptions={menuOptions} justify="right" {...rest}>
      <Flex alignItems="center" justifyContent="space-between">
        {enabled ? (
          staking ? (
            <StatusIndicator mr={2} variant="online" title="Wallet is staking" />
          ) : (
            <StatusIndicator
              mr={2}
              color="#fc0"
              title="Staking will start once there are eliglible coins."
            />
          )
        ) : (
          <StatusIndicator mr={2} title="Not staking" color="#ddd" />
        )}
        Staking
      </Flex>
    </Dropmenu>
  )
}

XStakingMenu.propTypes = {
  channelCount: PropTypes.number.isRequired,
  cryptoUnitName: PropTypes.string.isRequired,
  lightningBalance: PropTypes.number.isRequired,
  onchainBalance: PropTypes.number.isRequired,
  openModal: PropTypes.func.isRequired,
  pendingBalance: PropTypes.number.isRequired,
}

export default XStakingMenu
