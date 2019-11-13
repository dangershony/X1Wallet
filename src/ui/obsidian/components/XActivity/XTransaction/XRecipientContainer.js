import { connect } from 'react-redux'
import { tickerSelectors } from '../../../../renderer/reducers/ticker'
import { showXActivityModal } from '../../../reducers/fullNodeConnectionActions'

import XRecipient from './XRecipient'

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
)(XRecipient)
