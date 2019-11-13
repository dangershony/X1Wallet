import { connect } from 'react-redux'
import XStakingMenu from './XStakingMenu'
import { openModal } from 'reducers/modal'
import { callStopStaking, callUnlockWallet } from '../../reducers/fullNodeConnectionActions'
import { setIsPassphraseDialogOpen, setCloseAction } from '../../reducers/fullNodePassphrase'

const mapStateToProps = state => {
  return {
    cryptoUnitName: 'ODX',
    lightningBalance: state.fullNodeSettings.walletInfo.walletDetails.stakingInfo.stakingStatus.weight,
    pendingBalance: state.fullNodeSettings.walletInfo.walletDetails.stakingInfo.stakingStatus.networkWeight,
    onchainBalance: 2,
    channelCount: (state.fullNodeSettings.walletInfo.walletDetails.stakingInfo.stakingStatus.expectedTime/60).toFixed(0),
    enabled:  state.fullNodeSettings.walletInfo.walletDetails.stakingInfo.enabled,
    staking: state.fullNodeSettings.walletInfo.walletDetails.stakingInfo.stakingStatus.weight > 0,
    stakingInfo: state.fullNodeSettings.walletInfo.walletDetails.stakingInfo,
  }
}
/*
const mapStateToProps = state => {
  let cun = tickerSelectors.cryptoUnitName(state)
  return ({
    cryptoUnitName: tickerSelectors.cryptoUnitName(state),
    lightningBalance: balanceSelectors.channelBalanceConfirmed(state),
    pendingBalance: balanceSelectors.channelBalancePending(state),
    onchainBalance: balanceSelectors.walletBalance(state),
    channelCount: channelsSelectors.allChannelsCount(state),
  });
}
*/
const mapDispatchToProps = {
  openModal,
  callStopStaking,

  setIsPassphraseDialogOpen,
  setCloseAction,
  callUnlockWallet,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XStakingMenu)
