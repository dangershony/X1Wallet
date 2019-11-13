import get from 'lodash/get'
import { createSelector } from 'reselect'
import { grpcService } from 'workers'
import { openModal, closeModal } from '../../renderer/reducers/modal'
import { settingsSelectors } from '../../renderer/reducers/settings'
import { showError } from '../../renderer/reducers/notification'

import ApiService from '../crypto/ApiService'

// ------------------------------------
// Initial State
// ------------------------------------

const initialState = {
  addressesLoading: {
    np2wkh: false,
    p2wkh: false,
  },
  addresses: {
    np2wkh: null,
    p2wkh: 'bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4',
  },
  walletModal: false,

  allAddressesLoading: false,
  allAddresses: [],
  suggestedReceiveAddress: null,
}

// ------------------------------------
// Constants
// ------------------------------------

export const X1_FETCH_ALL_ADDRESSES = 'X1_FETCH_ALL_ADDRESSES'
export const X1_RECEIVE_ALL_ADDRESSES = 'X1_RECEIVE_ALL_ADDRESSES'
export const X1_SET_SUGGESTED_RECEIVEADDRESS = 'X1_SET_SUGGESTED_RECEIVEADDRESS'

export const NEW_ADDRESS = 'NEW_ADDRESS'
export const NEW_ADDRESS_SUCCESS = 'NEW_ADDRESS_SUCCESS'
export const NEW_ADDRESS_FAILURE = 'NEW_ADDRESS_FAILURE'
export const OPEN_WALLET_MODAL = 'OPEN_WALLET_MODAL'
export const CLOSE_WALLET_MODAL = 'CLOSE_WALLET_MODAL'

// LND expects types to be sent as int, so this object will allow mapping from string to int
const ADDRESS_TYPES = {
  p2wkh: 0,
  np2wkh: 1,
}

// ------------------------------------
// Actions
// ------------------------------------

export const openWalletModal = () => dispatch => dispatch(openModal('XRECEIVE_MODAL'))
export const closeWalletModal = () => dispatch => dispatch(closeModal('XRECEIVE_MODAL'))

export const xfetchAllAddresses = () => async (dispatch, getState) => {
  dispatch({ type: X1_FETCH_ALL_ADDRESSES })

  const arg = window.x1Tools.createRequestForWalletInStore('getReceiveAddresses', {})

  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      const { addresses } = responsePayload.responsePayload
      dispatch({ type: X1_RECEIVE_ALL_ADDRESSES, addresses })

      for (let i = 0; i < addresses.length; i++) {
        const { address, isUsed, isChange, encryptedPrivateKey } = addresses[i]
        if (!isUsed && !isChange) {
          dispatch({ type: X1_SET_SUGGESTED_RECEIVEADDRESS, suggestedReceiveAddress: address })
          break
        }
      }
    }
  })
}

// ------------------------------------
// Action Handlers
// ------------------------------------

const ACTION_HANDLERS = {
  [X1_FETCH_ALL_ADDRESSES]: state => {
    return { ...state, allAddressesLoading: true }
  },
  [X1_RECEIVE_ALL_ADDRESSES]: (state, { allAddresses }) => {
    return {
      ...state,
      allAddressesLoading: false,
      allAddresses,
    }
  },
  [X1_SET_SUGGESTED_RECEIVEADDRESS]: (state, { suggestedReceiveAddress }) => {
    return {
      ...state,
      suggestedReceiveAddress: suggestedReceiveAddress,
    }
  },
  [OPEN_WALLET_MODAL]: state => ({ ...state, walletModal: true }),
  [CLOSE_WALLET_MODAL]: state => ({ ...state, walletModal: false }),
}

// ------------------------------------
// Selectors
// ------------------------------------

const addressSelectors = {}

addressSelectors.addressesLoading = state => state.fullNodeAddress.allAddressesLoading
addressSelectors.currentAddresses = state => state.fullNodeAddress.addresses
addressSelectors.currentConfig = state => settingsSelectors.currentConfig(state)
addressSelectors.currentAddress = state => state.fullNodeAddress.suggestedReceiveAddress

addressSelectors.isAddressLoading = createSelector(
  addressSelectors.addressesLoading,
  addressesLoading => addressesLoading
)

export { addressSelectors }

// ------------------------------------
// Reducer
// ------------------------------------

/**
 * addressReducer - Address reducer.
 *
 * @param  {object} state = initialState Initial state
 * @param  {object} action Action
 * @returns {object} Next state
 */
export default function addressReducer(state = initialState, action) {
  const handler = ACTION_HANDLERS[action.type]

  return handler ? handler(state, action) : state
}
