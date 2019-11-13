import React from 'react'
import { Flex } from 'rebass'
import { Text } from 'components/UI'
import ZapLogo from '../../../renderer/components/Icon/ZapLogo'

const XWalletLogoComponent = ({networkName} ) => {
  return (
    <Flex alignItems="center" as="section">
     {/*  <ZapLogo height={28} width={28} /> */}
      {networkName && (
        <Text color="#eee" fontSize="xxl" fontWeight='100' ml={2}>
          {networkName.replace('Main','')}
        </Text>
      )}
    </Flex>
  )
}

export default XWalletLogoComponent
