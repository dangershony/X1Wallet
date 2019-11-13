import React from 'react'
import PropTypes from 'prop-types'
import { Box, Flex } from 'rebass'
import { CryptoValue, FiatValue } from 'containers/UI'
import { Button, Text } from 'components/UI'
import Qrcode from 'components/Icon/Qrcode'
import XCryptoSelector from './XCryptoSelector'
const XWalletBalanceComponent = ({ totalBalance, openWalletModal }) => (
  <Box as="section">
    <Flex alignItems="center">
      <Box mr={3} onClick={openWalletModal}>
        <Button variant="secondary">
          <Qrcode height="21px" width="21px" />
        </Button>
      </Box>

      <Box>
        <Flex alignItems="baseline">
          <Text fontSize="xxl">
            <CryptoValue value={totalBalance} />
          </Text>
          <XCryptoSelector ml={1} />
        </Flex>
        <Text color="gray">
          {'≈ '}
          <FiatValue style="currency" value={totalBalance} />
        </Text>
      </Box>
    </Flex>
  </Box>
)

XWalletBalanceComponent.propTypes = {
  openWalletModal: PropTypes.func.isRequired,
  totalBalance: PropTypes.number,
}

export default XWalletBalanceComponent
