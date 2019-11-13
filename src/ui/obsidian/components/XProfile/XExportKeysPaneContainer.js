import { connect } from 'react-redux'
import { showNotification } from 'reducers/notification'
import XExportKeysPaneComponent from './XExportKeysPaneComponent'
import {
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
} from '../../reducers/fullNodePassphrase'
import { callExportKeys } from '../../reducers/fullNodeKeys'

const mapStateToProps = state => {
  const { isExportingKeys, exportError, exportSuccessMessage, exportedKeys } = state.fullNodeKeys
  return {
    fullNodeSettings: state.fullNodeSettings,
    fullNodePassphrase: state.fullNodePassphrase,

    isExportingKeys,
    exportError,
    exportSuccessMessage,
    exportedKeys,
  }
}

const mapDispatchToProps = {
  showNotification,
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
  callExportKeys,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XExportKeysPaneComponent)
