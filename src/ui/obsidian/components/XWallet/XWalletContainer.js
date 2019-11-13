import { connect } from 'react-redux'
import { tickerSelectors } from 'reducers/ticker'
import { openWalletModal } from '../../reducers/fullNodeAddress'
import { openModal } from 'reducers/modal'
import XWalletComponent from './XWalletComponent'

const mapDispatchToProps = {
  openWalletModal,
  openModal,
}

const mapStateToProps = state => ({
  networkName: state.fullNodeConnection.daemonInfo.networkName,
  ticker: state.ticker,
  totalBalance: state.fullNodeSettings.walletInfo.walletDetails.balance.confirmed + state.fullNodeSettings.walletInfo.walletDetails.balance.pending,
  currentTicker: tickerSelectors.currentTicker(state),
  fullNodeConnection: state.fullNodeConnection,
})


export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XWalletComponent)


