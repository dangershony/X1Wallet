import React, { useEffect } from 'react'
import PropTypes from 'prop-types'
import { Redirect } from 'react-router-dom'
import { Bar, Panel } from 'components/UI'
import XAddressComponent from './XAddressComponent'
import XTutorialsComponent from './XTutorialsComponent'
import XProgressComponent from './XProgressComponent'
import XNewWalletHeaderComponent from './XNewWalletHeaderComponent'
import XOldWalletHeaderComponent from './XOldWalletHeaderComponent'

const XSyncingComponent = ({
  setIsWalletOpen,
  syncStatus,
  hasSynced,
  syncPercentage,
  recoveryPercentage,
  address,
  blockHeight,
  neutrinoBlockHeight,
  neutrinoCfilterHeight,
  neutrinoRecoveryHeight,
  isAddressLoading,
  isLightningGrpcActive,
  network,
  showNotification,
}) => {
  useEffect(() => {
    setIsWalletOpen(true)
  }, [setIsWalletOpen])

  if (isLightningGrpcActive && syncStatus === 'complete') {
    return <Redirect to="/app" />
  }

  return (
    <Panel width={1}>
      <Panel.Header mx="auto" width={9 / 16}>
        {hasSynced ? <XOldWalletHeaderComponent /> : <XNewWalletHeaderComponent network={network} />}
        <Bar my={3} />
      </Panel.Header>

      <Panel.Body mb={3} mx="auto" width={9 / 16}>
        {hasSynced ? (
          <XTutorialsComponent
            css={`
              height: 100%;
            `}
          />
        ) : (
          <XAddressComponent
            address={address}
            css={`
              height: 100%;
            `}
            isAddressLoading={isAddressLoading}
            showNotification={showNotification}
          />
        )}
      </Panel.Body>

      <Panel.Footer
        bg="secondaryColor"
        css={`
          min-height: 160px;
        `}
        p={3}
      >
        <XProgressComponent
          blockHeight={blockHeight}
          mx="auto"
          neutrinoBlockHeight={neutrinoBlockHeight}
          neutrinoCfilterHeight={neutrinoCfilterHeight}
          neutrinoRecoveryHeight={neutrinoRecoveryHeight}
          recoveryPercentage={recoveryPercentage}
          syncPercentage={syncPercentage}
          syncStatus={syncStatus}
          width={9 / 16}
        />
      </Panel.Footer>
    </Panel>
  )
}

XSyncingComponent.propTypes = {
  address: PropTypes.string,
  blockHeight: PropTypes.number,
  hasSynced: PropTypes.bool,
  isAddressLoading: PropTypes.bool,
  isLightningGrpcActive: PropTypes.bool,
  network: PropTypes.string,
  neutrinoBlockHeight: PropTypes.number,
  neutrinoCfilterHeight: PropTypes.number,
  neutrinoRecoveryHeight: PropTypes.number,
  recoveryPercentage: PropTypes.number,
  setIsWalletOpen: PropTypes.func.isRequired,
  showNotification: PropTypes.func.isRequired,
  syncPercentage: PropTypes.number,
  syncStatus: PropTypes.string.isRequired,
}

export default XSyncingComponent
