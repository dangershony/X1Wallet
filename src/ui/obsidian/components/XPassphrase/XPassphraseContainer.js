import { connect } from 'react-redux'

import XPassphrase from './XPassphrase'
import {
  setPassphrase,
  clearPassphrase,
  setIsPassphraseDialogOpen,
} from '../../reducers/fullNodePassphrase'

import { showError } from '../../../renderer/reducers/notification'

const mapStateToProps = state => ({
  isDialogOpen: state.fullNodePassphrase.isDialogOpen,
  passphrase: state.fullNodePassphrase.passphrase,
  closeAction: state.fullNodePassphrase.closeAction,
  passphraseChallenge: state.fullNodePassphrase.passphraseChallenge,
})
const cancel = () => {}
const mapDispatchToProps = {
  onCancel: cancel,
  setPassphrase,
  clearPassphrase,
  setIsPassphraseDialogOpen,
  showError,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XPassphrase)
