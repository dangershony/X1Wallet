import { connect } from 'react-redux'
import Dropdown from './XCryptoDropdown'
import { tickerSelectors, setCryptoUnit } from '../../../renderer/reducers/ticker'

const mapStateToProps = state => ({
  activeKey: tickerSelectors.cryptoUnit(state),
  items: tickerSelectors.cryptoUnits(state),
  valueField: 'name',
})

const mapDispatchToProps = {
  onChange: setCryptoUnit,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(Dropdown)
