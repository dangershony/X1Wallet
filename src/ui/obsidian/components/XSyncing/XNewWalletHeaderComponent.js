import React from 'react'
import PropTypes from 'prop-types'
import { Header, Link } from 'components/UI'
import { FormattedMessage } from 'react-intl'
import xmessages from './xmessages'

const XNewWalletHeaderComponent = ({ network }) => (
  <Header
    subtitle={
      network === 'testnet' && (
        <Link onClick={() => window.Zap.openTestnetFaucet()}>
          <FormattedMessage {...xmessages.fund_link} />
        </Link>
      )
    }
    title={<FormattedMessage {...xmessages.fund_heading} />}
  />
)

XNewWalletHeaderComponent.propTypes = {
  network: PropTypes.string,
}

export default XNewWalletHeaderComponent
