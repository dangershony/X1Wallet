import React from 'react'
import PropTypes from 'prop-types'
import { injectIntl } from 'react-intl'
import Refresh from 'components/Icon/Refresh'
import { ActionButton } from '../../../../renderer/components/UI'
import xmessages from './xmessages'

const ActivityRefresh = injectIntl(({ intl, onClick, ...rest }) => (
  <ActionButton
    hint={intl.formatMessage({ ...xmessages.refresh_button_hint })}
    onClick={onClick}
    {...rest}
  >
    <Refresh height="16px" width="16px" />
  </ActionButton>
))

ActivityRefresh.propTypes = {
  onClick: PropTypes.func.isRequired,
}

export default ActivityRefresh
