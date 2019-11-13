import { ResponsePayload, RequestObject } from './crypto/visualcrypt-dtos'

import createScheduler from './X1Scheduler'

window.scheduler =  createScheduler()
const scheduler = window.scheduler

const isActiveWalletFullNodeWallet = state => {
  const { activeWallet } = state.settings
  for (let i = 0; i < state.wallet.wallets.length; i++) {
    if (state.wallet.wallets[i].id === activeWallet) {
      if (state.wallet.wallets[i].type === 'fullNodeWallet') {
        return true
      } else {
        return false
      }
    }
    return false
  }
}

const translateCryptoUnitName = cryptoUnitName => {
  return cryptoUnitName
  const fullNodeCryptoUnitName = cryptoUnitName === 'BTC' ? 'ODX' : cryptoUnitName
  return fullNodeCryptoUnitName
}

const createRequestForWalletInStore = (command, payload) => {
  const request = new RequestObject(command, payload)
  const {
    fullNodeHost,
    fullNodeExpectedAuthKey,
    fullNodeUser,
    fullNodePwd,
    fullNodeSelectedWalletName,
  } = window.x1Store.getState().fullNodeConnection
  request.target = fullNodeSelectedWalletName
  request.ipAddress = fullNodeHost
  request.user = fullNodeUser
  request.password = fullNodePwd
  request.expectedAuthKey = fullNodeExpectedAuthKey
  return request
}

const isZap = () => {
  return window.x1Tools.history.location.pathname !== '/xapp'
}

const x1Tools = {
  isActiveWalletFullNodeWallet,
  translateCryptoUnitName,
  createRequestForWalletInStore,
  scheduler,
  history, // set in Root.js
  isZap,
}

export default x1Tools
