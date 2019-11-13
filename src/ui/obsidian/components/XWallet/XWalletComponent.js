import React from 'react'
import PropTypes from 'prop-types'
import { Flex } from 'rebass'
import { Card } from 'components/UI'
import XWalletBalanceComponent from './XWalletBalanceComponent'
import XWalletButtonsComponent from './XWalletButtonsComponent'
import XWalletMenuComponent from './XWalletMenuComponent'
import XWalletLogoComponent from './XWalletLogoComponent'

const XWalletComponent = ({ totalBalance, networkName, openWalletModal, openModal }) => {
  return (<Card bg="secondaryColor" p={0} pb={3} pt={4}>
    <Flex alignItems="flex-end" as="header" justifyContent="space-between" mt={2} px={4}>
      <XWalletLogoComponent networkName={networkName} />
      <XWalletMenuComponent openModal={openModal} />
    </Flex>

    <Flex alignItems="flex-end" as="header" justifyContent="space-between" mb={3} mt={4} px={5}>
      <XWalletBalanceComponent openWalletModal={openWalletModal} totalBalance={totalBalance} />
      <XWalletButtonsComponent openModal={openModal} />
    </Flex>
  </Card>);
}



export default XWalletComponent
