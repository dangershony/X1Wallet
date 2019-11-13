import ApiService from '../crypto/ApiService'
import {
  X1_FNC_RESET,
} from './fullNodeConnectionActions'

const X1_RECEIVE_TRANSACTIONRESPONSE = 'X1_RECEIVE_TRANSACTIONRESPONSE'

export const callBuildTransaction = (transactionRequest) => dispatch => {
  const arg = window.x1Tools.createRequestForWalletInStore('buildTransaction', transactionRequest)
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(receiveTransactionResponse(responsePayload.responsePayload))
    }
  })
}

const receiveTransactionResponse = transactionResponse => ({
  type: X1_RECEIVE_TRANSACTIONRESPONSE,
  transactionResponse,
})

  const initialState = {
    transactionResponse: { },
  }
  
  export default function fullNodeSend(state = initialState, action) {
    switch (action.type) {
      case X1_FNC_RESET:
        return Object.assign({}, initialState)
      case X1_RECEIVE_TRANSACTIONRESPONSE:
        return Object.assign({}, state, {
          transactionResponse: action.transactionResponse,
        })
      default:
        return state
    }
  }
  