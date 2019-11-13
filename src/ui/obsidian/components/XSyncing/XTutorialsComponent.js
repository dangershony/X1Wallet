import React from 'react'
import { Flex } from 'rebass'
import { Button, Text } from 'components/UI'
import { FormattedMessage } from 'react-intl'
import xmessages from './xmessages'

const XTutorialsComponent = props => (
  <Flex alignItems="center" flexDirection="column" justifyContent="center" {...props}>
    <Text my={3}>
      <FormattedMessage {...xmessages.tutorials_list_description} />
    </Text>
    <Button mx="auto" onClick={() => window.Zap.openHelpPage()} size="small">
      <FormattedMessage {...xmessages.tutorials_button_text} />
    </Button>
  </Flex>
)

export default XTutorialsComponent
