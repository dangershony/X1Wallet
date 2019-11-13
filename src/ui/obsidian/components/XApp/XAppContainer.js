import { connect } from 'react-redux'
import { setIsWalletOpen } from 'reducers/wallet'
import { setModals, modalSelectors } from 'reducers/modal'
import { initTickers } from 'reducers/ticker'

import {
  callHistoryInfo,
  callWalletInfo,
  callDaemonInfo,
} from '../../reducers/fullNodeConnectionActions'

import XAppComponent from './XAppComponent'

const mapStateToProps = state => {
  return {
    fullNodeConnection: state.fullNodeConnection,
    redirectPayReq: state.pay.redirectPayReq,
    modals: modalSelectors.getModalState(state),
  }
}

const mapDispatchToProps = {

  setIsWalletOpen,
  setModals,
  initTickers, // set USD | EUR | etc., call fetchTickers

  callHistoryInfo,
  callWalletInfo,
  callDaemonInfo,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XAppComponent)
