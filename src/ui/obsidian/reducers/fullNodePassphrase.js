import { vcl } from '../crypto/visualcrypt-light'

// Action Types
export const X1_PASSPHRASE_SET = 'X1_PASSPHRASE_SET'
export const X1_PASSPHRASECHALLENGE_SET = 'X1_PASSPHRASECHALLENGE_SET'
export const X1_PASSPHRASE_CLEAR = 'X1_PASSPHRASE_CLEAR'
export const X1_PASSPHASE_SETDIALOGOPEN = 'X1_PASSPHASE_SETDIALOGOPEN'
export const X1_PASSPHRASE_SETCLOSEACTION = 'X1_PASSPHRASE_SETCLOSEACTION'

// Action Creators
// Action creators simply return an action:
export const setPassphrase = passphrase => ({
  type: X1_PASSPHRASE_SET,
  passphrase,
})

export const setPassphraseChallenge = passphraseChallengeHexString => {
  const passphraseChallenge = vcl.hexStringToBytes(passphraseChallengeHexString)
  return {
    type: X1_PASSPHRASECHALLENGE_SET,
    passphraseChallenge,
  }
}

export const clearPassphrase = () => ({
  type: X1_PASSPHRASE_CLEAR,
})

export const setIsPassphraseDialogOpen = isDialogOpen => ({
  type: X1_PASSPHASE_SETDIALOGOPEN,
  isDialogOpen,
})

export const setCloseAction = closeAction => ({
  type: X1_PASSPHRASE_SETCLOSEACTION,
  closeAction,
})

const closeAction = () => {
  console.log('CLOSE ACTION TRIGGERED')
}

const initialState = {
  isDialogOpen: false,
  passphrase: '',
  passphraseChallenge: null,
  setAt: null,
  closeAction: closeAction,
}

export default function fullNodePassphrase(state = initialState, action) {
  switch (action.type) {
    case 'X1_FNC_RESET': // use string to avoid a circular dependency
      return Object.assign({}, initialState)
    case X1_PASSPHRASE_SET:
      return Object.assign({}, state, {
        passphrase: action.passphrase,
        setAt: Date.now(),
      })
    case X1_PASSPHRASECHALLENGE_SET:
      return Object.assign({}, state, {
        passphraseChallenge: action.passphraseChallenge,
      })
    case X1_PASSPHRASE_CLEAR:
      return Object.assign({}, state, {
        passphrase: '',
        setAt: null,
      })
    case X1_PASSPHASE_SETDIALOGOPEN:
      return Object.assign({}, state, {
        isDialogOpen: action.isDialogOpen,
      })
    case X1_PASSPHRASE_SETCLOSEACTION:
      return Object.assign({}, state, {
        closeAction: action.closeAction,
      })
    default:
      return state
  }
}
