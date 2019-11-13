import { createSelector } from 'reselect'
import { showSystemNotification } from '@zap/utils/notifications'
import { convert } from '@zap/utils/btc'
import delay from '@zap/utils/delay'
import errorToUserFriendly from '@zap/utils/userFriendlyErrors'

import ApiService from '../crypto/ApiService'

// ------------------------------------
// Initial State
// ------------------------------------

const initialState = {
  transactionLoading: false,
  transactions: [],
  transactionsSending: [],

  isQueryingFees: false,
  onchainFees: {
    fast: null,
    medium: null,
    slow: null,
  },
  queryFeesError: null,

  currentTransactionSending: null,
  isSendingTransaction: false,
  sendTransactionError: null,
}

// ------------------------------------
// Constants
// ------------------------------------

export const X1_FETCH_TRANSACTIONS = 'X1_FETCH_TRANSACTIONS'
export const X1_RECEIVE_TRANSACTIONS = 'X1_RECEIVE_TRANSACTIONS'

export const X1_FETCH_FEEESTIMATE = 'X1_FETCH_FEEESTIMATE'
export const X1_RECEIVE_FEEESTIMATE = 'X1_RECEIVE_FEEESTIMATE'
export const X1_ERROR_FEEESTIMATE = 'X1_ERROR_FEEESTIMATE'

export const X1_SEND_TX = 'X1_SEND_TX'
export const X1_SEND_TX_SUCCESS = 'X1_SEND_TX_SUCCESS'
export const X1_SEND_TX_ERROR = 'X1_SEND_TX_ERROR'

export const SEND_TRANSACTION = 'SEND_TRANSACTION'
export const TRANSACTION_SUCCESSFUL = 'TRANSACTION_SUCCESSFUL'
export const TRANSACTION_FAILED = 'TRANSACTION_FAILED'
export const TRANSACTION_COMPLETE = 'TRANSACTION_COMPLETE'
export const ADD_TRANSACTION = 'ADD_TRANSACTION'

// ------------------------------------
// Helpers
// ------------------------------------

/**
 * decorateTransaction - Decorate transaction object with custom/computed properties.
 *
 * @param  {object} transaction Transaction
 * @returns {object} Decorated transaction
 */
const decorateTransaction = transaction => {
  const decoration = {
    type: 'transaction',
    received: transaction.amount > 0,
  }
  return {
    ...transaction,
    ...decoration,
  }
}

// ------------------------------------
// Actions
// ------------------------------------

/**
 * sendTransaction - Store details of sending in progress onchain transaction.
 *
 * @param  {object} data Transaction data
 * @returns {object} Action
 */
export function sendTransaction(data) {
  const transaction = {
    ...data,
    status: 'sending',
    isSending: true,
    time_stamp: Math.round(new Date() / 1000),
  }
  return {
    type: SEND_TRANSACTION,
    transaction,
  }
}

/**
 * fetchTransactions - Fetch details of all transactions.
 *
 * @returns {Function} Thunk
 */
export const xfetchTransactions = () => async dispatch => {
  dispatch({ type: X1_FETCH_TRANSACTIONS })
  const arg = window.x1Tools.createRequestForWalletInStore('history', {
    walletName: '',
    accountName: '',
    skip: null,
    take: null,
    q: '',
  })
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      const { history } = responsePayload.responsePayload
      const { transactionsHistory } = history[0]
      let transactions = []
      for (let i = 0; i < transactionsHistory.length; i++) {
        const {
          amount,
          blockIndex,
          confirmedInBlock,
          id,
          payments,
          timestamp,
          toAddress,
          type, // received, staked, send
        } = transactionsHistory[i]

        let isReceived = type === 'received' || type === 'staked' ? true : false

        transactions[i] = {
          type: 'transaction', // transaction, payment, invoice
          time_stamp: timestamp,
          tx_hash: id,
          amount: isReceived ? amount.satoshi : amount.satoshi * -1,
          sending: false,
          status: 'successful',
          error: '',
          isStaked: type === 'staked',
        }
      }
      const outer = { transactions: transactions }
      dispatch(receiveTransactions(outer))
    }
  })
}

/**
 * receiveTransactions - Success callback for fetch transactions.
 *
 * @param {{transactions}} List of transaction.
 * @returns {Function} Thunk
 */
export const receiveTransactions = ({ transactions }) => (dispatch, getState) => {
  const state = getState()

  if (state.fullNodeTransactions.currentTransactionSending) {
    for (let i = 0; i < transactions.length; i++) {
      let { tx_hash } = transactions[i]
      if (tx_hash === state.fullNodeTransactions.currentTransactionSending.tx_hash) {
        const currentTransactionSending = null
        dispatch({ type: X1_SEND_TX, currentTransactionSending })
      }
    }
  }

  /*
  const currentAddresses = addressSelectors.currentAddresses(state)
  let usedAddresses = []

  // Keep track of used addresses.
  transactions.forEach(transaction => {
    usedAddresses = usedAddresses.concat(transaction.dest_addresses)
  })
*/
  dispatch({ type: X1_RECEIVE_TRANSACTIONS, transactions })

  /*
  // If our current wallet address has been used, generate a new one.
  Object.entries(currentAddresses).forEach(([type, address]) => {
    if (usedAddresses.includes(address)) {
      dispatch(newAddress(type))
    }
  })

  // fetch new balance
  dispatch(fetchBalance())
  */
}

/**
 * fetchTransactions - Fetch details of all transactions.
 *
 * @returns {Function} Thunk
 */
export const xfetchFeeEstimate = (amount, destinationAddress) => async dispatch => {
  dispatch({ type: X1_FETCH_FEEESTIMATE })
  const arg = window.x1Tools.createRequestForWalletInStore('estimateFee', {
    walletName: '',
    accountName: '',
    allowUnconfirmed: true,
    feeType: 'medium',
    recipients: [
      { amount: (amount / 100000000).toString(), destinationAddress: destinationAddress },
    ],
  })
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      const onchainFees = {
        fast: responsePayload.responsePayload.satoshi,
        medium: responsePayload.responsePayload.satoshi,
        slow: responsePayload.responsePayload.satoshi,
      }
      dispatch({ type: X1_RECEIVE_FEEESTIMATE, onchainFees })
    } else {
      dispatch({
        type: X1_ERROR_FEEESTIMATE,
        queryFeesError: responsePayload.status + ' - ' + responsePayload.statusText,
      })
    }
  })
}

/**
 * fetchTransactions - Fetch details of all transactions.
 *
 * @returns {Function} Thunk
 */
export const xsendTransaction = (amount, destinationAddress, feeSatoshi) => async (
  dispatch,
  getState
) => {
  const currentTransactionSending = {
    // for the fullNode
    amount: amount,
    destinationAddress: destinationAddress,
    feeSatoshi: feeSatoshi,

    // for the wallet UI
    type: 'transaction',
    time_stamp: Date.now(),
    sending: true,
    status: 'failed',

    isStaked: false,

    // from the fullNode
    tx_hash: null,
    hex: null,
    error: null,
  }
  const { passphrase } = getState().fullNodePassphrase
  dispatch({ type: X1_SEND_TX, currentTransactionSending })
  const arg = window.x1Tools.createRequestForWalletInStore('buildAndSendTransaction', {
    walletName: '',
    accountName: '',
    allowUnconfirmed: true,
    feeAmount: feeSatoshi / 100000000,
    recipients: [{ amount: amount / 100000000, destinationAddress: destinationAddress }],
    password: passphrase,
    shuffleOutputs: false,
    opReturnAmount: undefined,
    opReturnData: undefined,
  })
  return ApiService.makeRequest(arg).then(async responsePayload => {
    if (responsePayload.status === 200) {
      currentTransactionSending.tx_hash = responsePayload.responsePayload.transactionId
      currentTransactionSending.hex = responsePayload.responsePayload.hex
      currentTransactionSending.status = 'sending'

      // Ensure payment stays in sending state for at least 2 seconds.
      //await delay(5000 - (Date.now() - currentTransactionSending.time_stamp * 1000))

      // Mark the payment as successful.
      dispatch({ type: X1_SEND_TX_SUCCESS, currentTransactionSending })

      // Wait for another second.
      //await delay(1000)

      // Mark the payment as successful.
      //dispatch({ type: TRANSACTION_COMPLETE, addr })
    } else {
      currentTransactionSending.error = responsePayload.status + ' - ' + responsePayload.statusText
      currentTransactionSending.status = 'failed'
      dispatch({ type: X1_SEND_TX_ERROR, currentTransactionSending })
    }
  })
}

/**
 * sendCoins - Send an onchain transaction.
 *
 * @param  {object}  options Options
 * @param  {number}  options.value Number of units to send
 * @param  {string}  options.addr Destination address
 * @param  {string}  options.cryptoUnit Crypto unit that value is denominated in (converted to sats prior to send)
 * @param  {number}  options.targetConf Number of blocks to target for conf time
 * @param  {number}  options.satPerByte Sat per byte fee rate to apply
 * @param  {boolean} options.isCoinSweep Boolean indicating whether this is a coin sweep (will send all funds).
 * @returns {Function} Thunk
 */
export const sendCoins = ({
  value,
  addr,
  cryptoUnit,
  targetConf,
  satPerByte,
  isCoinSweep,
}) => async dispatch => {
  // backend needs amount in satoshis no matter what currency we are using
  const amount = convert(cryptoUnit, 'sats', value)

  // Add to sendingPayments in the state.
  const payload = {
    amount: isCoinSweep ? null : amount,
    addr,
    target_conf: targetConf,
    sat_per_byte: satPerByte,
    send_all: isCoinSweep,
  }
  dispatch(sendTransaction(payload))

  // Submit the transaction to LND.
  try {
    const grpc = await grpcService
    const { txid } = await grpc.services.Lightning.sendCoins(payload)
    dispatch(transactionSuccessful({ ...payload, txid }))
  } catch (e) {
    dispatch(
      transactionFailed({
        error: e.message,
        addr: payload.addr,
      })
    )
  }
}

/**
 * transactionSuccessful - Success handler for sendCoins.
 *
 * @param  {{ string }} addr Destination address
 * @returns {Function} Thunk
 */
export const transactionSuccessful = ({ addr }) => async (dispatch, getState) => {
  const state = getState()
  const { timestamp } = state.transaction.transactionsSending.find(t => t.addr === addr)

  // Ensure payment stays in sending state for at least 2 seconds.
  await delay(2000 - (Date.now() - timestamp * 1000))

  // Mark the payment as successful.
  dispatch({ type: TRANSACTION_SUCCESSFUL, addr })

  // Wait for another second.
  await delay(1000)

  // Mark the payment as successful.
  dispatch({ type: TRANSACTION_COMPLETE, addr })
}

/**
 * transactionSuccessful - Error handler for sendCoins.
 *
 * @param  {object} details Details
 * @param  {{ string }} details.addr Destination address
 * @param  {{ string }} details.error Error message
 * @returns {Function} Thunk
 */
export const transactionFailed = ({ addr, error }) => async (dispatch, getState) => {
  const state = getState()
  const { timestamp } = state.transaction.transactionsSending.find(t => t.addr === addr)

  // Ensure payment stays in sending state for at least 2 seconds.
  await delay(2000 - (Date.now() - timestamp * 1000))

  // Mark the payment as failed.
  dispatch({ type: TRANSACTION_FAILED, addr, error: errorToUserFriendly(error) })
}

/**
 * receiveTransactionData - Listener for when a new transaction is pushed from the subscriber.
 *
 * @param  {object} transaction Transaction
 * @returns {Function} Thunk
 */
export const receiveTransactionData = transaction => (dispatch, getState) => {
  // add the transaction only if we are not already aware of it
  const state = getState()
  if (
    !state.transaction ||
    !state.transaction.transactions ||
    !state.transaction.transactions.find(tx => tx.tx_hash === transaction.tx_hash)
  ) {
    dispatch({ type: ADD_TRANSACTION, transaction })

    // Refetch transactions.
    dispatch(fetchTransactions())

    // fetch updated channels
    dispatch(fetchChannels())

    // HTML 5 desktop notification for the new transaction
    if (transaction.received) {
      showSystemNotification(
        'On-chain Transaction Received!',
        "Lucky you, you just received a new on-chain transaction. I'm jealous."
      )
      dispatch(newAddress(settingsSelectors.currentConfig(state).address)) // Generate a new address
    } else {
      showSystemNotification(
        'On-chain Transaction Sent!',
        "Hate to see 'em go but love to watch 'em leave. Your on-chain transaction successfully sent."
      )
    }
  }
}

// ------------------------------------
// Action Handlers
// ------------------------------------

const ACTION_HANDLERS = {
  [X1_FETCH_TRANSACTIONS]: state => {
    return { ...state, transactionLoading: true }
  },
  [SEND_TRANSACTION]: (state, { transaction }) => {
    return {
      ...state,
      transactionsSending: [...state.transactionsSending, transaction],
    }
  },
  [X1_RECEIVE_TRANSACTIONS]: (state, { transactions }) => {
    return {
      ...state,
      transactionLoading: false,
      transactions,
    }
  },

  [X1_FETCH_FEEESTIMATE]: state => ({
    ...state,
    isQueryingFees: true,
    onchainFees: {},
    queryFeesError: null,
  }),
  [X1_RECEIVE_FEEESTIMATE]: (state, { onchainFees }) => ({
    ...state,
    isQueryingFees: false,
    onchainFees,
    queryFeesError: null,
  }),
  [X1_ERROR_FEEESTIMATE]: (state, { error }) => ({
    ...state,
    isQueryingFees: false,
    onchainFees: {},
    queryFeesError: error,
  }),

  [X1_SEND_TX]: state => ({
    ...state,
    isSendingTransaction: true,
    currentTransactionSending: null,
    sendTransactionError: null,
  }),
  [X1_SEND_TX_SUCCESS]: (state, { currentTransactionSending }) => ({
    ...state,
    isSendingTransaction: false,
    currentTransactionSending: currentTransactionSending,
    sendTransactionError: null,
  }),
  [X1_SEND_TX_ERROR]: (state, { error }) => ({
    ...state,
    isSendingTransaction: false,
    currentTransactionSending: null,
    sendTransactionError: error,
  }),

  [ADD_TRANSACTION]: (state, { transaction }) => ({
    ...state,
    transactions: [transaction, ...state.transactions],
  }),
  [TRANSACTION_SUCCESSFUL]: (state, { addr }) => {
    return {
      ...state,
      transactionsSending: state.transactionsSending.map(item => {
        if (item.addr !== addr) {
          return item
        }
        return {
          ...item,
          status: 'successful',
        }
      }),
    }
  },
  [TRANSACTION_FAILED]: (state, { addr, error }) => {
    return {
      ...state,
      transactionsSending: state.transactionsSending.map(item => {
        if (item.addr !== addr) {
          return item
        }
        return {
          ...item,
          status: 'failed',
          error,
        }
      }),
    }
  },
  [TRANSACTION_COMPLETE]: (state, { addr }) => {
    return {
      ...state,
      transactionsSending: state.transactionsSending.filter(item => item.addr !== addr),
    }
  },
}

// ------------------------------------
// Selectors
// ------------------------------------

const fullNodeTransactionsSelectors = {}
const transactionsSelector = state => state.fullNodeTransactions.transactions
const transactionsSendingSelector = state =>
  state.fullNodeTransactions.currentTransactionSending
    ? [state.fullNodeTransactions.currentTransactionSending]
    : []

fullNodeTransactionsSelectors.transactionsSending = createSelector(
  transactionsSendingSelector,
  transactionsSending => transactionsSending.map(transaction => decorateTransaction(transaction))
)

fullNodeTransactionsSelectors.transactions = createSelector(
  transactionsSelector,
  transactions => {
    return transactions.map(transaction => decorateTransaction(transaction))
  }
)

export { fullNodeTransactionsSelectors }

// ------------------------------------
// Reducer
// ------------------------------------

/**
 * transactionReducer - Transaction reducer.
 *
 * @param  {object} state = initialState Initial state
 * @param  {object} action Action
 * @returns {object} Next state
 */
export default function fullNodeTransactions(state = initialState, action) {
  const handler = ACTION_HANDLERS[action.type]
  return handler ? handler(state, action) : state
}
