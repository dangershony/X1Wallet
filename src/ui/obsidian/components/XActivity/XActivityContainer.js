import { connect } from 'react-redux'
import XActivityComponent from './XActivityComponent'
import { tickerSelectors } from '../../../renderer/reducers/ticker'
import { showXActivityModal } from '../../reducers/fullNodeConnectionActions'
import { openModal } from 'reducers/modal'

const mapStateToProps = state => {
  
  return {
    historyInfo: state.fullNodeConnection.historyInfo,
    cryptoUnitName: tickerSelectors.cryptoUnitName(state),
    consensusTipHeight: getHeight(state),
    connectionInfo: state.fullNodeSettings.walletInfo.connectionInfo
  }
}
const getHeight = state => {
  const height =
    (state.fullNodeSettings.walletInfo &&
      state.fullNodeSettings.walletInfo.consensusTipHeight &&
      state.fullNodeSettings.walletInfo.consensusTipHeight > 0 &&
      state.fullNodeSettings.walletInfo.consensusTipHeight) ||
    NaN

  return height
}
const mapDispatchToProps = {
  showXActivityModal,
  openModal,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XActivityComponent)
