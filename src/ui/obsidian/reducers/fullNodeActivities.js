import { createSelector } from 'reselect'
import { openModal, closeModal } from '../../renderer/reducers/modal'
//import { fetchDescribeNetwork } from './network'
import { xfetchTransactions, fullNodeTransactionsSelectors } from './fullNodeTransactions'
import { xfetchAllAddresses } from './fullNodeAddress'
import { callBalance } from './fullNodeConnectionActions'
//import { fetchPayments, paymentSelectors } from './payment'
//import { fetchInvoices, invoiceSelectors } from './invoice'
//import { fetchBalance } from './balance'
//import { fetchChannels } from './channels'

// ------------------------------------
// Initial State
// ------------------------------------

const initialState = {
  filter: 'ALL_ACTIVITY',
  filters: [
    { key: 'ALL_ACTIVITY' },
    { key: 'SENT_ACTIVITY' },
    { key: 'RECEIVED_ACTIVITY' },
    { key: 'STAKING_ACTIVITY' },
    { key: 'EXPIRED_ACTIVITY' },
    { key: 'INTERNAL_ACTIVITY' },
  ],
  modal: {
    itemType: null,
    itemId: null,
  },
  searchText: null,
  isActivityLoading: false,
  activityLoadingError: null,
}

// ------------------------------------
// Constants
// ------------------------------------

export const SHOW_ACTIVITY_MODAL = 'SHOW_ACTIVITY_MODAL'
export const HIDE_ACTIVITY_MODAL = 'HIDE_ACTIVITY_MODAL'
export const CHANGE_FILTER = 'CHANGE_FILTER'
export const UPDATE_SEARCH_TEXT = 'UPDATE_SEARCH_TEXT'
export const X1_FETCH_ACTIVITY_HISTORY = 'X1_FETCH_ACTIVITY_HISTORY'
export const X1_FETCH_ACTIVITY_HISTORY_SUCCESS = 'X1_FETCH_ACTIVITY_HISTORY_SUCCESS'
export const X1_FETCH_ACTIVITY_HISTORY_FAILURE = 'X1_FETCH_ACTIVITY_HISTORY_FAILURE'

export const X1_SHOW_ACTIVITY_MODAL = 'X1_SHOW_ACTIVITY_MODAL'
export const X1_HIDE_ACTIVITY_MODAL = 'X1_HIDE_ACTIVITY_MODAL'

// ------------------------------------
// Helpers
// ------------------------------------

// getMonth() returns the month in 0 index (0 for Jan), so we create an arr of the
// string representation we want for the UI
const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

/**
 * propMatches - Check whether a prop exists and contains a given search string.
 *
 * @param  {string}  prop Prop name
 * @returns {boolean} Boolean indicating if the prop was found and contains the search string
 */
const propMatches = function(prop) {
  const { item, searchTextSelector = '' } = this
  return item[prop] && item[prop].toLowerCase().includes(searchTextSelector.toLowerCase())
}

/**
 * invoiceExpired - Check whether an invoice is expired.
 *
 * @param  {object}  invoice Invoice
 * @returns {boolean} Boolean indicating if the invoice has expired
 */
const invoiceExpired = invoice => {
  const expiresAt = parseInt(invoice.creation_date, 10) + parseInt(invoice.expiry, 10)
  return expiresAt < Math.round(new Date() / 1000)
}

/**
 * returnTimestamp - Returns invoice, payment or transaction timestamp.
 *
 * @param  {object} activity Activity item
 * @returns {string} Timestamp
 */
function returnTimestamp(activity) {
  switch (activity.type) {
    case 'transaction':
      return activity.time_stamp
    case 'invoice':
      return activity.settled ? activity.settle_date : activity.creation_date
    case 'payment':
      return activity.creation_date
  }
}

/**
 * groupAll - Sorts data by date and inserts grouping titles.
 *
 * @param {Array} data Items to group
 * @returns {Array} Groups items
 */
function groupAll(data) {
  // according too https://stackoverflow.com/a/11252167/3509860
  // this provides an accurate measurement including handling of DST
  const daysBetween = (t1, t2) => Math.round((t2 - t1) / 86400)
  return data
    .sort((a, b) => b.timestamp - a.timestamp)
    .reduce((acc, next) => {
      const prev = acc[acc.length - 1]
      //check if need insert a group title
      if (prev) {
        const days = daysBetween(next.timestamp, prev.timestamp)
        if (days >= 1) {
          acc.push({ title: next.date })
        }
      } else {
        //This is a very first row. Insert title here too
        acc.push({ title: next.date })
      }
      acc.push(next)
      return acc
    }, [])
}

/**
 * applySearch - Filter activity list by checking various properties against a given search string.
 *
 * @param  {Array}  data Activity item list
 * @param  {string} searchTextSelector Search text
 * @returns {Array}  Filtered activity list
 */
const applySearch = (data, searchTextSelector) => {
  if (!searchTextSelector) {
    return data
  }

  return data.filter(item => {
    // Check basic props for a match.
    const hasPropMatch = [
      'date',
      'type',
      'memo',
      'tx_hash',
      'payment_hash',
      'payment_preimage',
      'payment_request',
      'dest_node_pubkey',
      'dest_node_alias',
    ].some(propMatches, { item, searchTextSelector })

    // Check every destination address.
    const hasAddressMatch =
      item.dest_addresses && item.dest_addresses.find(addr => addr.includes(searchTextSelector))

    // Include the item if at least one search criteria matches.
    return hasPropMatch || hasAddressMatch
  })
}

const prepareData = (data, searchText) => {
  return groupAll(applySearch(data, searchText))
}

// ------------------------------------
// Actions
// ------------------------------------

/**
 * showActivityModal - Show the activity modal with a given activity item.
 *
 * @param {string} itemType Item type
 * @param {string} itemId Item id
 * @returns {Function} Thunk
 */
export const showActivityModal = (itemType, itemId) => dispatch => {
  dispatch({ type: SHOW_ACTIVITY_MODAL, itemType, itemId })
  dispatch(openModal('ACTIVITY_MODAL'))
}



/**
 * hideActivityModal - Hide the activity modal.
 *
 * @returns {Function} Thunk
 */
export const hideActivityModal = () => dispatch => {
  dispatch({ type: HIDE_ACTIVITY_MODAL })
  dispatch(closeModal('ACTIVITY_MODAL'))
}

/**
 * hideXActivityModal - Hide the activity modal.
 *
 * @returns {Function} Thunk
 */
export const hideXActivityModal = () => dispatch => {
  dispatch({ type: X1_HIDE_ACTIVITY_MODAL })
  dispatch(closeModal('XACTIVITY_MODAL'))
}

/**
 * changeFilter - Set the current activity filter.
 *
 * @param {string} filter Filter to apply
 * @returns {object} Action
 */
export function changeFilter(filter) {
  return {
    type: CHANGE_FILTER,
    filter,
  }
}

/**
 * updateSearchText - Set the current activity search string.
 *
 * @param {string} searchText Search string to apply
 * @returns {object} Action
 */
export function updateSearchText(searchText = null) {
  return {
    type: UPDATE_SEARCH_TEXT,
    searchText,
  }
}

/**
 * fetchActivityHistory - Fetch user activity history, including Balance, Payments, Invoices, Transactions etc.
 *
 * @returns {Function} Thunk
 */
export const xfetchActivityHistory = () => dispatch => {
  dispatch({ type: X1_FETCH_ACTIVITY_HISTORY })
  try {
    /*dispatch(fetchDescribeNetwork())
    dispatch(fetchChannels())
    dispatch(fetchBalance())
    dispatch(fetchPayments())*/
    dispatch(callBalance())
    dispatch(xfetchAllAddresses())
    dispatch(xfetchTransactions())
    dispatch({ type: X1_FETCH_ACTIVITY_HISTORY_SUCCESS })
  } catch (error) {
    dispatch({ type: X1_FETCH_ACTIVITY_HISTORY_FAILURE, error })
  }
}

// ------------------------------------
// Action Handlers
// ------------------------------------

const ACTION_HANDLERS = {
  [SHOW_ACTIVITY_MODAL]: (state, { itemType, itemId }) => ({
    ...state,
    modal: { itemType, itemId },
  }),
  [HIDE_ACTIVITY_MODAL]: state => ({ ...state, modal: { itemType: null, itemId: null } }),

  // Obsidian modals
  [X1_SHOW_ACTIVITY_MODAL]: (state, { itemType, itemId }) => ({
    ...state,
    modal: { itemType, itemId },
  }),
  [X1_HIDE_ACTIVITY_MODAL]: state => ({ ...state, modal: { itemType: null, itemId: null } }),

  [CHANGE_FILTER]: (state, { filter }) => ({ ...state, filter }),
  [UPDATE_SEARCH_TEXT]: (state, { searchText }) => ({ ...state, searchText }),
  [X1_FETCH_ACTIVITY_HISTORY]: state => ({ ...state, isActivityLoading: true }),
  [X1_FETCH_ACTIVITY_HISTORY_SUCCESS]: state => ({ ...state, isActivityLoading: false }),
  [X1_FETCH_ACTIVITY_HISTORY_FAILURE]: (state, { error }) => ({
    ...state,
    isActivityLoading: false,
    activityLoadingError: error,
  }),
}

// ------------------------------------
// Selectors
// ------------------------------------

const fullNodeActivitySelectors = {}
const filterSelector = state => {
  return state.fullNodeActivities.filter
}
const filtersSelector = state => state.fullNodeActivities.filters
const searchTextSelector = state => state.fullNodeActivities.searchText
const modalItemTypeSelector = state => state.fullNodeActivities.modal.itemType
const modalItemIdSelector = state => state.fullNodeActivities.modal.itemId
//const paymentsSelector = state => paymentSelectors.payments(state)
//const paymentsSendingSelector = state => paymentSelectors.paymentsSending(state)
//const invoicesSelector = state => invoiceSelectors.invoices(state)
const fullNodeTransactionsSelector = state => fullNodeTransactionsSelectors.transactions(state)
const fullNodeTransactionsSendingSelector = state =>
  fullNodeTransactionsSelectors.transactionsSending(state)

fullNodeActivitySelectors.filter = filterSelector
fullNodeActivitySelectors.filters = filtersSelector
fullNodeActivitySelectors.searchText = searchTextSelector

/**
 * Map sending transactions to something that looks like normal transactions.
 */
const transactionsSending = createSelector(
  fullNodeTransactionsSendingSelector,
  transactionsSending => {
    const transactions = transactionsSending.map(transaction => {
      return {
        type: 'transaction',
        time_stamp: transaction.timestamp,
        amount: transaction.amount,
        sending: true,
        status: transaction.status,
        error: transaction.error,
      }
    })
    return transactions
  }
)

fullNodeActivitySelectors.activityModalItem = createSelector(
  //paymentsSelector,
  //invoicesSelector,
  fullNodeTransactionsSelector,
  modalItemTypeSelector,
  modalItemIdSelector,
  (/*payments, invoices, */ transactions, itemType, itemId) => {
    switch (itemType) {
      case 'INVOICE':
        return invoices.find(invoice => invoice.payment_request === itemId)
      case 'TRANSACTION':
        return transactions.find(transaction => transaction.tx_hash === itemId)
      case 'PAYMENT':
        return payments.find(payment => payment.payment_hash === itemId)
      default:
        return null
    }
  }
)

// decorates activity entry with date and timestamp fields
const addDate = entry => {
  const timestamp = returnTimestamp(entry)
  const d = new Date(timestamp * 1000)
  const date = d.getDate()
  return { ...entry, date: `${months[d.getMonth()]} ${date}, ${d.getFullYear()}`, timestamp }
}

// All activity: pre-search
const allActivityRaw = createSelector(
  //paymentsSendingSelector,
  transactionsSending,
  //paymentsSelector,
  fullNodeTransactionsSelector,
  //invoicesSelector,
  (/*paymentsSending, */ transactionsSending, /*payments,*/ transactions /*invoices*/) => {
    return [
      //...paymentsSending,
      ...transactionsSending,
      //...payments,
      ...transactions.filter(
        transaction => !transaction.isFunding && !transaction.isClosing && !transaction.isPending
      ),
      //...invoices.filter(invoice => invoice.settled || !invoiceExpired(invoice)),
    ].map(addDate)
  }
)

// All activity: post search
const allActivity = createSelector(
  searchTextSelector,
  allActivityRaw,
  (searchText, activity) => {
    return prepareData(activity, searchText)
  }
)

// Sent activity: pre-search
const sentActivityRaw = createSelector(
  //paymentsSendingSelector,
  transactionsSending,
  //paymentsSelector,
  fullNodeTransactionsSelector,
  (/*paymentsSending, */ transactionsSending, /* payments, */ transactions) => {
    return [
      //...paymentsSending,
      ...transactionsSending,
      //...payments,
      ...transactions.filter(transaction => {
        return !transaction.received
        /*&&
              !transaction.isFunding &&
              !transaction.isClosing &&
              !transaction.isPending;*/
      }),
    ].map(addDate)
  }
)

// Sent activity: post-search
const sentActivity = createSelector(
  searchTextSelector,
  sentActivityRaw,
  (searchText, activity) => prepareData(activity, searchText)
)

// Received activity: pre-search
const receivedActivityRaw = createSelector(
  //invoicesSelector,
  fullNodeTransactionsSelector,
  (/*invoices,*/ transactions) => {
    return [
      //...invoices.filter(invoice => invoice.settled),
      ...transactions.filter(
        transaction =>
          transaction.received &&
          !transaction.isFunding &&
          !transaction.isClosing &&
          !transaction.isPending
      ),
    ].map(addDate)
  }
)

// Received activity: post-search
const receivedActivity = createSelector(
  searchTextSelector,
  receivedActivityRaw,
  (searchText, activity) => prepareData(activity, searchText)
)

// Pending activity: pre-search
const stakingActivityRaw = createSelector(
  fullNodeTransactionsSelector,
  transactions => {
    return [
      ...transactions.filter(transaction => {
        return transaction.isStaked
      }),
    ].map(addDate)
  }
)

// Pending activity: post-search
const stakingActivity = createSelector(
  searchTextSelector,
  stakingActivityRaw,
  (searchText, activity) => prepareData(activity, searchText)
)

// Expired activity: pre-search
const expiredActivityRaw = createSelector(
  //invoicesSelector,
  invoices => {
    return invoices.filter(invoice => !invoice.settled && invoiceExpired(invoice)).map(addDate)
  }
)

// Expired activity: post-search
const expiredActivity = createSelector(
  searchTextSelector,
  expiredActivityRaw,
  (searchText, activity) => prepareData(activity, searchText)
)

// Internal activity: pre-search
const internalActivityRaw = createSelector(
  fullNodeTransactionsSelector,
  transactions => {
    return transactions
      .filter(
        transaction => transaction.isFunding || (transaction.isClosing && !transaction.isPending)
      )
      .map(addDate)
  }
)

// Internal activity: post-search
const internalActivity = createSelector(
  searchTextSelector,
  internalActivityRaw,
  (searchText, activity) => prepareData(activity, searchText)
)

const FILTERS = {
  ALL_ACTIVITY: allActivity,
  SENT_ACTIVITY: sentActivity,
  RECEIVED_ACTIVITY: receivedActivity,
  STAKING_ACTIVITY: stakingActivity,
  EXPIRED_ACTIVITY: expiredActivity,
  INTERNAL_ACTIVITY: internalActivity,
}

fullNodeActivitySelectors.currentActivity = createSelector(
  filterSelector,
  filter => FILTERS[filter]
)

export { fullNodeActivitySelectors }

// ------------------------------------
// Reducer
// ------------------------------------

/**
 * fullNodeActivities - Activity reducer.
 *
 * @param  {object} state = initialState Initial state
 * @param  {object} action Action
 * @returns {object} Next state
 */
export default function fullNodeActivities(state = initialState, action) {
  const handler = ACTION_HANDLERS[action.type]

  return handler ? handler(state, action) : state
}
