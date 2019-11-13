import { connect } from 'react-redux'
import XWalletReceiveModalComponent from './XWalletReceiveModalComponent'
import { showNotification } from 'reducers/notification'

const mapStateToProps = state => ({
  fullNodeConnection: state.fullNodeConnection,
  fullNodeSettings: state.fullNodeSettings,
})

const mapDispatchToProps = {
  showNotification,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XWalletReceiveModalComponent)
