import React from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage } from 'react-intl'
import { Flex } from 'rebass'
import { Dropdown, Label } from '../../../../renderer/components/UI'
import xmessages from './xmessages'

const messageMapper = key => {
  const filters = {
    ALL_ACTIVITY: xmessages.actiity_filter_all,
    SENT_ACTIVITY: xmessages.actiity_filter_sent,
    RECEIVED_ACTIVITY: xmessages.actiity_filter_received,
    STAKING_ACTIVITY: xmessages.activity_filter_staking,
    EXPIRED_ACTIVITY: xmessages.actiity_filter_expired,
    INTERNAL_ACTIVITY: xmessages.actiity_filter_internal,
  }

  return filters[key]
}

const XActivityFilter = ({ changeFilter, filter, filters, ...rest }) => (
  <Flex alignItems="baseline" {...rest}>
    <Label fontWeight="light" htmlFor="channel-filter" mr={2}>
      <FormattedMessage {...xmessages.actiity_filter_label} />
    </Label>
    <Dropdown
      activeKey={filter}
      highlightOnValid={false}
      id="activity-filter"
      items={filters}
      messageMapper={messageMapper}
      onChange={changeFilter}
    />
  </Flex>
)

XActivityFilter.propTypes = {
  changeFilter: PropTypes.func.isRequired,
  filter: PropTypes.string,
  filters: PropTypes.array,
}

XActivityFilter.defaultProps = {
  filters: [],
}

export default XActivityFilter
