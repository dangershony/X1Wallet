import { connect } from 'react-redux'
import { walletSelectors } from 'reducers/wallet'
import XProfilePageComponent from './XProfilePageComponent'

const mapStateToProps = state => ({
  activeWalletSettings: walletSelectors.activeWalletSettings(state),
  networkName: state.fullNodeConnection.daemonInfo.networkName,
})

export default connect(mapStateToProps)(XProfilePageComponent)
