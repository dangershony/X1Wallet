import { connect } from 'react-redux'
import { tickerSelectors } from '../../../../renderer/reducers/ticker'
import { showXActivityModal } from '../../../reducers/fullNodeConnectionActions'

import XTransaction from './XTransaction'

const mapDispatchToProps = {
  showXActivityModal,
}

const mapStateToProps = state => {
  const props = {
    cryptoUnitName: tickerSelectors.cryptoUnitName(state),
  }
  return props
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XTransaction)
