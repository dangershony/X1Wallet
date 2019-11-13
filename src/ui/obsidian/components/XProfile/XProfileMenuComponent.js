import React from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage } from 'react-intl'
import { Menu } from 'components/UI'
import { PANE_NODEINFO, PANE_STAKINGINFO, PANE_GENERALINFO, PANE_IMPORTKEYS, PANE_EXPORTKEYS, PANE_RESCAN } from './xconstants'
import xmessages from './xmessages'

const XProfileMenuComponent = ({ group, setGroup, ...rest }) => {
  const nodeInfoLink = {
    id: PANE_NODEINFO,
    title: <FormattedMessage {...xmessages.nodeinfo_pane_title} />,
    onClick: () => setGroup(PANE_NODEINFO),
  }
  const generalInfoLink = {
    id: PANE_GENERALINFO,
    title: <FormattedMessage {...xmessages.generalinfo_pane_title} />,
    onClick: () => setGroup(PANE_GENERALINFO),
  }
  const stakingInfoLink = {
    id: PANE_STAKINGINFO,
    title: <FormattedMessage {...xmessages.stakinginfo_pane_title} />,
    onClick: () => setGroup(PANE_STAKINGINFO),
  }

  const importKeysLink = {
    id: PANE_IMPORTKEYS,
    title: <FormattedMessage {...xmessages.importkeys_pane_title} />,
    onClick: () => setGroup(PANE_IMPORTKEYS),
  }

  const exportKeysLink = {
    id: PANE_EXPORTKEYS,
    title: <FormattedMessage {...xmessages.exportkeys_pane_title} />,
    onClick: () => setGroup(PANE_EXPORTKEYS),
  }

  const rescanLink = {
    id: PANE_RESCAN,
    title: <FormattedMessage {...xmessages.rescan_pane_title} />,
    onClick: () => setGroup(PANE_RESCAN),
  }

  const items = [nodeInfoLink, stakingInfoLink, generalInfoLink, importKeysLink, exportKeysLink, rescanLink]

  return <Menu items={items} selectedItem={group} {...rest} />
}

XProfileMenuComponent.propTypes = {
  group: PropTypes.string,
  setGroup: PropTypes.func.isRequired,
}

export default XProfileMenuComponent
