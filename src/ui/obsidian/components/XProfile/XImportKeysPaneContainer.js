import { connect } from 'react-redux'
import { showNotification } from 'reducers/notification'
import XImportKeysPaneComponent from './XImportKeysPaneComponent'
import {
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
} from '../../reducers/fullNodePassphrase'
import { callImportKeys, setPastedText } from '../../reducers/fullNodeKeys'

const mapStateToProps = state => {
  const {
    isImportingKeys,
    pastedText,
    importError,
    importSuccessMessage,
    importedAddresses,
  } = state.fullNodeKeys
  return {
    fullNodeSettings: state.fullNodeSettings,
    fullNodePassphrase: state.fullNodePassphrase,
    
    isImportingKeys,
    pastedText,
    importError,
    importSuccessMessage,
    importedAddresses,
  }
}

const mapDispatchToProps = {
  showNotification,
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
  callImportKeys,
  setPastedText,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XImportKeysPaneComponent)
