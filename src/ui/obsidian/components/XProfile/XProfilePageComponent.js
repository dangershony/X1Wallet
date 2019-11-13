import React, { useState } from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage } from 'react-intl'
import { Heading, MainContent, Panel, Sidebar } from 'components/UI'
import ZapLogo from 'components/Icon/ZapLogo'

import XNodeInfoPaneContainer from './XNodeInfoPaneContainer'
import XGeneralInfoPaneContainer from './XGeneralInfoPaneContainer'
import XStakingInfoPaneContainer from './XStakingInfoPaneContainer'
import XImportKeysPaneContainer from './XImportKeysPaneContainer'

import XExportKeysPaneContainer from './XExportKeysPaneContainer'
import XRescanPaneContainer from './XRescanPaneContainer'

import XProfileMenuComponent from './XProfileMenuComponent'
import {
  PANE_NODEINFO,
  PANE_STAKINGINFO,
  PANE_GENERALINFO,
  PANE_IMPORTKEYS,
  PANE_EXPORTKEYS,
  PANE_RESCAN,
  DEFAULT_PANE,
} from './xconstants'
import xmessages from './xmessages'
import XWalletLogoComponent from './../XWallet/XWalletLogoComponent'

const XProfilePageComponent = ({ options, networkName, ...rest }) => {
  const { pane } = options

  const [group, setGroup] = useState(pane ? pane : DEFAULT_PANE)

  let title
  switch (group) {
    case PANE_NODEINFO:
      title = xmessages.nodeinfo_pane_title
      break
    case PANE_STAKINGINFO:
      title = xmessages.stakinginfo_pane_title
      break
    default:
      title = xmessages.advanced_page_title
      break
  }
  return (
    <>
      <Sidebar.medium pt={40}>
        <Panel>
          <Panel.Header mb={40} px={4}>
            <XWalletLogoComponent networkName={networkName} />
          </Panel.Header>
          <Panel.Body css={{ 'overflow-y': 'overlay' }}>
            <XProfileMenuComponent group={group} p={2} setGroup={setGroup} />
          </Panel.Body>
        </Panel>
      </Sidebar.medium>

      <MainContent pb={2} pl={5} pr={6} pt={4}>
        <Heading.h1 fontSize={50} mb={2}>
          <FormattedMessage {...title} />
        </Heading.h1>
        {group === PANE_NODEINFO && <XNodeInfoPaneContainer />}
        {group === PANE_GENERALINFO && <XGeneralInfoPaneContainer />}
        {group === PANE_STAKINGINFO && <XStakingInfoPaneContainer />}
        {group === PANE_IMPORTKEYS && <XImportKeysPaneContainer />}
        {group === PANE_EXPORTKEYS && <XExportKeysPaneContainer />}
        {group === PANE_RESCAN && <XRescanPaneContainer />}
      </MainContent>
    </>
  )
}

XProfilePageComponent.propTypes = {
  activeWalletSettings: PropTypes.object.isRequired,
}

export default XProfilePageComponent
