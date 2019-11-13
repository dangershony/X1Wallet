import { createSelector } from 'reselect'
import get from 'lodash/get'
import { currencies, getDefaultCurrency } from '@zap/i18n'
import { requestTickerWithFallback } from '@zap/utils/rateProvider'
import { infoSelectors } from './info'
import { putConfig, settingsSelectors } from './settings'

// ------------------------------------
// Initial State
// ------------------------------------

const initialState = {
  tickerLoading: false,
  rates: null,
  fiatTicker: getDefaultCurrency(),
  fiatTickers: currencies,
  cryptoUnits: {
    bitcoin: [
      {
        key: 'btc',
        value: 'BTC',
      },
      {
        key: 'bits',
        value: 'bits',
      },
      {
        key: 'sats',
        value: 'satoshis',
      },
    ],
    litecoin: [
      {
        key: 'ltc',
        value: 'LTC',
      },
      {
        key: 'phots',
        value: 'photons',
      },
      {
        key: 'lits',
        value: 'litoshis',
      },
    ],
    obsidianx: [
      {
        key: 'odx',
        value: 'ODX',
      },
      {
        key: 'sats',
        value: 'satoshis',
      },
    ],
  },
}

// ------------------------------------
// Constants
// ------------------------------------

export const GET_TICKERS = 'GET_TICKERS'
export const RECIEVE_TICKERS = 'RECIEVE_TICKERS'

// ------------------------------------
// Actions
// ------------------------------------

/**
 * initCurrency - Initialise the fiat currency.
 *
 * @returns {Function} Thunk
 */
export const initCurrency = () => async (dispatch, getState) => {
  const state = getState()
  const currentConfig = settingsSelectors.currentConfig(state)
  const currentCurrency = tickerSelectors.fiatTicker(state)

  if (currentConfig.currency !== currentCurrency) {
    await dispatch(setFiatTicker(currentConfig.currency))
  }
}

/**
 * initCurrency - Initialise the fiat tickers.
 *
 * @returns {Function} Thunk
 */
export const initTickers = () => async (dispatch, getState) => {
  const state = getState()
  const currentConfig = settingsSelectors.currentConfig(state)
  const currentTicker = fiatTickerSelector(state)

  if (currentConfig.currency !== currentTicker) {
    dispatch(setFiatTicker(currentConfig.currency))
  }

  await dispatch(fetchTickers())
}

/**
 * setCryptoUnit - Set the currently active crypto unit.
 *
 * @param {string} unit Crypto unit (eg bits, sats, btc)
 * @returns {Function} Thunk
 */
export const setCryptoUnit = unit => async (dispatch, getState) => {
  const state = getState()
  const currentConfig = settingsSelectors.currentConfig(state)
  const chain = chainSelector(state)
  const savedUnit = currentConfig.units[chain]
  if (unit !== savedUnit) {
    await dispatch(putConfig(`units.${chain}`, unit))
  }
}

/**
 * setFiatTicker - Set the currently active fiat ticker.
 *
 * @param {string} fiatTicker Fiat ticker symbol (USD, EUR etc)
 * @returns {Function} Thunk
 */
export const setFiatTicker = fiatTicker => async dispatch => {
  await dispatch(putConfig('currency', fiatTicker))
  dispatch(fetchTickers())
}

/**
 * fetchTickers - Fetch all fiat tickers from currently active fiat ticker provider.
 *
 * @returns {Function} Thunk
 */
export const fetchTickers = () => async (dispatch, getState) => {
  const state = getState()

  const chain = infoSelectors.chainSelector(state)

  let coin

  switch (chain) {
    case 'bitcoin':
      coin = 'BTC'
      break
    case 'litecoin':
      coin = 'LTC'
      break
    case 'obsidianx':
      coin = 'ODX'
      break
  }
  const currentConfig = settingsSelectors.currentConfig(state)
  const currency = fiatTickerSelector(state)
  dispatch({ type: GET_TICKERS })
  let rates = await requestTickerWithFallback(currentConfig.rateProvider, coin, currency)
  dispatch({ type: RECIEVE_TICKERS, rates })
}

// ------------------------------------
// Action Handlers
// ------------------------------------

const ACTION_HANDLERS = {
  [GET_TICKERS]: state => ({ ...state, tickerLoading: true }),
  [RECIEVE_TICKERS]: (state, { rates }) => ({
    ...state,
    tickerLoading: false,
    rates,
  }),
}

// ------------------------------------
// Selectors
// ------------------------------------

const cryptoUnitsSelector = state => state.ticker.cryptoUnits
const ratesSelector = state => state.ticker.rates
const fiatTickerSelector = state => settingsSelectors.currentConfig(state).currency
const fiatTickersSelector = state => state.ticker.fiatTickers
const tickerLoadingSelector = state => state.ticker.tickerLoading
const chainSelector = state => state.info.chain
const networkSelector = state => state.info.network
const networksSelector = state => state.info.networks
const networkInfoSelector = createSelector(
  chainSelector,
  networkSelector,
  networksSelector,
  (chain, network, networks) => get(networks, `${chain}.${network}`)
)

const tickerSelectors = {}

tickerSelectors.tickerLoading = tickerLoadingSelector
tickerSelectors.fiatTicker = fiatTickerSelector
tickerSelectors.fiatTickers = fiatTickersSelector

tickerSelectors.cryptoUnit = createSelector(
  settingsSelectors.currentConfig,
  chainSelector,
  (currentConfig, chain) => {
    const cryptoUnit = get(currentConfig, `units.${chain}`, null)
    return cryptoUnit
  }
)

tickerSelectors.currentTicker = createSelector(
  ratesSelector,
  rates => rates || {}
)

tickerSelectors.cryptoUnits = createSelector(
  chainSelector,
  networkInfoSelector,
  cryptoUnitsSelector,
  (chain, networkInfo, cryptoUnits) => {
    return cryptoUnits[chain].map(item => ({
      ...item,
      name: `${networkInfo.unitPrefix}${item.value}`,
    }))
  }
)

// selector for currency address name e.g BTC, tBTC etc
tickerSelectors.cryptoAddressName = createSelector(
  chainSelector,
  tickerSelectors.cryptoUnits,
  (chain, cryptoUnits = []) => {
    // assume first entry is as a currency ticker name (e.g BTC, LTC etc)
    const [selectedUnit] = cryptoUnits
    if (selectedUnit) {
      return selectedUnit.name
    }
    // fallback in case something is very wrong
    return chain
  }
)

tickerSelectors.cryptoUnitName = createSelector(
  tickerSelectors.cryptoUnit,
  tickerSelectors.cryptoUnits,
  (unit, cryptoUnits = []) => {
    const selectedUnit = cryptoUnits.find(c => c.key === unit)
    return selectedUnit.name
  }
)

/**
 * Returns autopay limit currency unit name
 */
tickerSelectors.autopayCurrencyName = createSelector(
  tickerSelectors.cryptoUnits,
  cryptoUnits => cryptoUnits && cryptoUnits[cryptoUnits.length - 1].value
)

export { tickerSelectors }

// ------------------------------------
// Reducer
// ------------------------------------

/**
 * tickerReducer - Ticker reducer.
 *
 * @param  {object} state = initialState Initial state
 * @param  {object} action Action
 * @returns {object} Next state
 */
export default function tickerReducer(state = initialState, action) {
  const handler = ACTION_HANDLERS[action.type]

  return handler ? handler(state, action) : state
}
