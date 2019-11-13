import { connect } from 'react-redux'
import { showNotification } from 'reducers/notification'
import XRescanPaneComponent from './XRescanPaneComponent'
import {
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
} from '../../reducers/fullNodePassphrase'
import { callRescan } from '../../reducers/fullNodeKeys'

const mapStateToProps = state => {
  const { isRescanRequested, rescanError, rescanSuccessMessage } = state.fullNodeKeys
  return {
    fullNodePassphrase: state.fullNodePassphrase,

    isRescanRequested,
    rescanError,
    rescanSuccessMessage,
  }
}

const mapDispatchToProps = {
  showNotification,
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
  callRescan,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XRescanPaneComponent)
