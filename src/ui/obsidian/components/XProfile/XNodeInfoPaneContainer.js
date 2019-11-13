import { connect } from 'react-redux'
import { showNotification } from 'reducers/notification'
import XNodeInfoPaneComponent from './XNodeInfoPaneComponent'

const mapStateToProps = state => ({
  fullNodeSettings: state.fullNodeSettings,
  fullNodeConnection: state.fullNodeConnection,
})

const mapDispatchToProps = {
  showNotification,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XNodeInfoPaneComponent)
