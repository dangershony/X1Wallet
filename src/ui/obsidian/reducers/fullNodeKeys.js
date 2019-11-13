import { X1_FNC_RESET } from './fullNodeConnectionActions'

import ApiService from '../crypto/ApiService'

// Action Types
export const X1_KEYS_IMPORTING = 'X1_KEYS_IMPORTING'
export const X1_KEYS_SETPASTEDTEXT = 'X1_KEYS_SETPASTEDTEXT'
export const X1_KEYS_RECEIVEIMPORTRESULT = 'X1_KEYS_RECEIVEIMPORTRESULT'

export const X1_KEYS_EXPORTING = 'X1_KEYS_EXPORTING'
export const X1_KEYS_RECEIVEEXPORTRESULT = 'X1_KEYS_RECEIVEEXPORTRESULT'

export const X1_RESCAN_REQUESTED = 'X1_RESCAN_REQUESTED'
export const X1_RESCAN_RECEIVERESULT = 'X1_RESCAN_RECEIVERESULT'

// Action Creators
// Action creators simply return an action:
export const setKeysImporting = () => ({
  type: X1_KEYS_IMPORTING,
})

export const setPastedText = pastedText => ({
  type: X1_KEYS_SETPASTEDTEXT,
  pastedText,
})

export const receiveImportResult = (importError, importSuccessMessage, importedAddresses) => ({
  type: X1_KEYS_RECEIVEIMPORTRESULT,
  importError,
  importSuccessMessage,
  importedAddresses,
})

export const setKeysExporting = () => ({
  type: X1_KEYS_EXPORTING,
})

export const receiveExportResult = (exportError, exportSuccessMessage, exportedKeys) => ({
  type: X1_KEYS_RECEIVEEXPORTRESULT,
  exportError: exportError,
  exportSuccessMessage: exportSuccessMessage,
  exportedKeys: exportedKeys,
})

export const setRescanRequested = () => ({
  type: X1_RESCAN_REQUESTED,
})

export const receiveRescanResult = (rescanError, rescanSuccessMessage) => ({
  type: X1_RESCAN_RECEIVERESULT,
  rescanError: rescanError,
  rescanSuccessMessage: rescanSuccessMessage,
})

export const callRescan = () => (dispatch, getState) => {
  dispatch(setRescanRequested())
  const walletPassphrase = getState().fullNodePassphrase.passphrase
  const arg = window.x1Tools.createRequestForWalletInStore('syncFromDate', {
    walletPassphrase: walletPassphrase,
    date: '2009-01-03T18:15Z',
  })

  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(
        receiveRescanResult(
          '',
          "Rescan started from 2009-01-03T18:15Z",
        )
      )
    } else {
      dispatch(
        receiveRescanResult(responsePayload.status + ' - ' + responsePayload.statusText, '')
      )
    }
  })
}

export const callImportKeys = pastedText => (dispatch, getState) => {
  dispatch(setKeysImporting())
  const walletPassphrase = getState().fullNodePassphrase.passphrase
  const arg = window.x1Tools.createRequestForWalletInStore('importKeys', {
    walletPassphrase: walletPassphrase,
    keys: pastedText,
  })

  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(
        receiveImportResult(
          '',
          responsePayload.responsePayload.message,
          responsePayload.responsePayload.importedAddresses
        )
      )
    } else {
      dispatch(
        receiveImportResult(responsePayload.status + ' - ' + responsePayload.statusText, '', [])
      )
    }
  })
}

export const callExportKeys = () => (dispatch, getState) => {
  dispatch(setKeysExporting())
  const walletPassphrase = getState().fullNodePassphrase.passphrase
  const arg = window.x1Tools.createRequestForWalletInStore('exportKeys', {
    walletPassphrase: walletPassphrase,
  })

  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(
        receiveExportResult(
          '',
          responsePayload.responsePayload.message,
          responsePayload.responsePayload.exportedAddresses
        )
      )
    } else {
      dispatch(
        receiveExportResult(responsePayload.status + ' - ' + responsePayload.statusText, '', [])
      )
    }
  })
}

const initialState = {
  // import keys
  isImportingKeys: false,
  pastedText: '',
  importError: '',
  importSuccessMessage: '',
  importedAddresses: [],

  // export keys
  isExportingKeys: false,
  exportError: '',
  exportSuccessMessage: '',
  exportedKeys: [],

  // rescan
  isRescanRequested: false,
  rescanError: '',
  rescanSuccessMessage: '',
}

export default function fullNodePassphrase(state = initialState, action) {
  switch (action.type) {
    case X1_FNC_RESET:
      return Object.assign({}, initialState)
    case X1_KEYS_IMPORTING:
      return Object.assign({}, state, {
        isImportingKeys: true,
      })
    case X1_KEYS_SETPASTEDTEXT:
      return Object.assign({}, state, {
        pastedText: action.pastedText,
      })
    case X1_KEYS_RECEIVEIMPORTRESULT:
      return Object.assign({}, state, {
        isImportingKeys: false,
        importError: action.importError,
        importSuccessMessage: action.importSuccessMessage,
        importedAddresses: action.importedAddresses,
      })
    case X1_KEYS_EXPORTING:
      return Object.assign({}, state, {
        isExportingKeys: true,
      })
    case X1_KEYS_RECEIVEEXPORTRESULT:
      return Object.assign({}, state, {
        isExportingKeys: false,
        exportError: action.exportError,
        exportSuccessMessage: action.exportSuccessMessage,
        exportedKeys: action.exportedKeys,
      })
    case X1_RESCAN_REQUESTED:
      return Object.assign({}, state, {
        isRescanRequested: true,
      })
    case X1_RESCAN_RECEIVERESULT:
      return Object.assign({}, state, {
        isRescanRequested: false,
        rescanError: action.rescanError,
        rescanSuccessMessage: action.rescanSuccessMessage,
      })
    default:
      return state
  }
}
