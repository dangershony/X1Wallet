import { connect } from 'react-redux'
import { hideActivityModal, fullNodeActivitySelectors } from '../../../reducers/fullNodeActivities'
import { infoSelectors } from 'reducers/info'
import { showNotification } from 'reducers/notification'
import { XActivityModal } from './XActivityModal'
import { tickerSelectors } from '../../../../renderer/reducers/ticker'
const mapStateToProps = state => ({
  item: state.fullNodeConnection.historySelectedItem,
  network: infoSelectors.networkSelector(state),
  networkInfo: mockNetworkInfo(),
  cryptoUnitName: tickerSelectors.cryptoUnitName(state),
})

const mapDispatchToProps = {
  hideActivityModal,
  showNotification,
}
const mockNetworkInfo = () => {
  return {
    bitcoinJsNetwork: null,
    explorerUrl: 'http://explorer.obsidianproject.org',
    explorerUrls: {
      blockstream: 'https://blockstream.info',
      blockcypher: 'https://live.blockcypher.com/btc',
      smartbit: 'https://www.smartbit.com.au',
    },
    id: 'mainnet',
    name: 'Mainnet',
    unitPrefix: '',
  }
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XActivityModal)


