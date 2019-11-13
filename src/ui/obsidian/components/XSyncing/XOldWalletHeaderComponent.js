import React from 'react'
import { Header } from 'components/UI'
import { FormattedMessage } from 'react-intl'
import xmessages from './xmessages'

const XOldWalletHeaderComponent = () => (
  <Header
    subtitle={<FormattedMessage {...xmessages.sync_description} />}
    title={<FormattedMessage {...xmessages.sync_title} />}
  />
)

export default XOldWalletHeaderComponent
