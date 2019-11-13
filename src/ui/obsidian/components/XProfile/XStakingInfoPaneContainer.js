import { connect } from 'react-redux'
import { showNotification } from 'reducers/notification'
import XStakingInfoPaneComponent from './XStakingInfoPaneComponent'

const mapStateToProps = state => ({
  fullNodeSettings: state.fullNodeSettings,
})

const mapDispatchToProps = {
  showNotification,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XStakingInfoPaneComponent)
