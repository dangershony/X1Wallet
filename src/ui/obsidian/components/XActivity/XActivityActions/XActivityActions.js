import React from 'react'
import PropTypes from 'prop-types'
import { injectIntl, intlShape } from 'react-intl'
import { Flex } from 'rebass'
import { Card } from '../../../../renderer/components/UI'
import XActivityFilter from './XActivityFilter'
import XActivityRefresh from './XActivityRefresh'
import XActivitySearch from './XActivitySearch'

import xmessages from './xmessages'

const XActivityActions = ({
  filter,
  filters,
  searchText,
  changeFilter,
  xfetchActivityHistory,
  updateSearchText,
  intl,
  ...rest
}) => (
  <Card px={3} py={2} width={1} {...rest}>
    <Flex alignItems="center" as="section" justifyContent="space-between">
      <XActivitySearch
        placeholder={intl.formatMessage({ ...xmessages.search_placeholder })}
        searchText={searchText}
        updateSearchText={updateSearchText}
        width={1}
      />
      <Flex alignItems="center" as="section" justifyContent="flex-end">
        <XActivityFilter changeFilter={changeFilter} filter={filter} filters={filters} mx={3} />

        <XActivityRefresh mx={3} onClick={xfetchActivityHistory} />
      </Flex>
    </Flex>
  </Card>
)

XActivityActions.propTypes = {
  changeFilter: PropTypes.func.isRequired,
  xfetchActivityHistory: PropTypes.func.isRequired,
  filter: PropTypes.string.isRequired,
  filters: PropTypes.array.isRequired,
  intl: intlShape.isRequired,
  searchText: PropTypes.string,
  updateSearchText: PropTypes.func.isRequired,
}

export default injectIntl(XActivityActions)
