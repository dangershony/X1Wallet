import { connect } from 'react-redux'
import XPayComponent from './XPayComponent'
import { fetchTickers, tickerSelectors } from 'reducers/ticker'
import { closeModal } from 'reducers/modal'
import { infoSelectors } from 'reducers/info'
import { callBuildTransaction } from '../../reducers/fullNodeSend'

import {
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
} from '../../reducers/fullNodePassphrase'

const mapStateToProps = state => {
  return ({
    chain: state.info.chain,
    network: state.info.network,
    chainName: infoSelectors.chainName(state),
    cryptoUnit: tickerSelectors.cryptoUnit(state),
    cryptoUnitName: tickerSelectors.cryptoUnitName(state),
    
    transactionResponse: state.fullNodeSend.transactionResponse,
    walletInfo: state.fullNodeSettings.walletInfo,
    fullNodeIsLoading: state.fullNodeConnection.fullNodeIsLoading,
    passphrase: state.fullNodePassphrase.passphrase
  });
}

const mapDispatchToProps = {
  closeModal,
  fetchTickers,
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
  callBuildTransaction,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XPayComponent)
