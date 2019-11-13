import ApiService from '../crypto/ApiService'
import { RequestObject } from '../crypto/visualcrypt-dtos'
import { putWallet } from '../../renderer/reducers/wallet'
import { setInfo } from '../../renderer/reducers/info'
import { setPassphraseChallenge, clearPassphrase } from './fullNodePassphrase'
import { openModal, closeModal } from '../../renderer/reducers/modal'

export const X1_FNC_SETHOST = 'X1_FNC_SETHOST'
export const X1_FNC_SETERRORTEXT = 'X1_FNC_SETERRORTEXT'
export const X1_FNC_SETEXPECTEDAUTHKEY = 'X1_FNC_SETEXPECTEDAUTHKEY'
export const X1_FNC_SETCREDENTIALSREQUIRED = 'X1_FNC_SETCREDENTIALSREQUIRED'
export const X1_FNC_SETUSER = 'X1_FNC_SETUSER'
export const X1_FNC_SETPWD = 'X1_FNC_SETPWD'
export const X1_FNC_SETCREDENTIALSSUCCESS = 'X1_FNC_SETCREDENTIALSSUCCESS'
export const X1_RECEIVE_DAEMONINFO = 'X1_RECEIVE_DAEMONINFO'
export const X1_FNC_SETWALLETNAME = 'X1_FNC_SETWALLETNAME'
export const X1_FNC_RESET = 'X1_FNC_RESET'

export const X1_FNC_SETCRYPTOUNIT = 'X1_FNC_SETCRYPTOUNIT'
export const X1_RECEIVE_HISTORYINFO = 'X1_RECEIVE_HISTORYINFO'
export const X1_FNC_GENERALINFO = 'X1_FNC_GENERALINFO'
export const X1_RECEIVE_WALLETINFO = 'X1_RECEIVE_WALLETINFO'
export const X1_FNC_SETISLOADING = 'X1_FNC_SETISLOADING'
export const X1_FNC_TOGGLE_CREATENEWWALLET = 'X1_FNC_TOGGLE_CREATENEWWALLET'
export const X1_FNC_SETNEWWALLETNAME = 'X1_FNC_SETNEWWALLETNAME'
export const X1_FNC_SETNEWWALLETPASSPHRASE = 'X1_FNC_SETNEWWALLETPASSPHRASE'
export const X1_FNC_SETWALLETCREATERESULT = 'X1_FNC_SETWALLETCREATERESULT'
export const X1_FNC_NODESTATUS = 'X1_FNC_NODESTATUS'

export const X1_SHOW_ACTIVITY_MODAL = 'X1_SHOW_ACTIVITY_MODAL'
export const X1_HIDE_ACTIVITY_MODAL = 'X1_HIDE_ACTIVITY_MODAL'

export const selectWallet = fullNodeSelectedWalletName => {
  return {
    type: X1_FNC_SETWALLETNAME,
    fullNodeSelectedWalletName: fullNodeSelectedWalletName,
  }
}

export const walletCreateResult = fullNodeWalletCreateSuccess => ({
  type: X1_FNC_SETWALLETCREATERESULT,
  fullNodeWalletCreateSuccess: fullNodeWalletCreateSuccess,
})

export const receiveDaemonInfo = (daemonInfo) => ({
  type: X1_RECEIVE_DAEMONINFO,
  daemonInfo,
})

export const setErrorText = (errorText = '') => ({
  type: X1_FNC_SETERRORTEXT,
  errorText,
})

export const setExpectedAuthKey = authKey => ({
  type: X1_FNC_SETEXPECTEDAUTHKEY,
  authKey: authKey,
})

export const setIsCredentialsRequired = isCredentialsRequired => ({
  type: X1_FNC_SETCREDENTIALSREQUIRED,
  isCredentialsRequired,
})

export const setHost = (ipAddress, isSuccess, authKey) => ({
  type: X1_FNC_SETHOST,
  ipAddress: ipAddress,
  fullNodeIsConnectionSuccess: isSuccess && authKey ? true : false,
  authKey: isSuccess && authKey ? authKey : '',
})

export const setUser = user => ({
  type: X1_FNC_SETUSER,
  user,
})

export const setPwd = pwd => ({
  type: X1_FNC_SETPWD,
  pwd,
})

export const setIsCredentialsSuccess = isSuccess => ({
  type: X1_FNC_SETCREDENTIALSSUCCESS,
  isSuccess,
})

export const setCryptoUnit = unit => ({
  type: X1_FNC_SETCRYPTOUNIT,
  cryptoUnit: unit,
})

export const receiveBalance = (satoshisConfirmed, satoshisUnconfirmed, satoshisSpendable) => ({
  type: X1_RECEIVE_TRANSACTIONRESPONSE,
  satoshisConfirmed: satoshisConfirmed,
  satoshisUnconfirmed: satoshisUnconfirmed,
  satoshisSpendable: satoshisSpendable,
})

export const receiveHistoryInfo = historyInfo => ({
  type: X1_RECEIVE_HISTORYINFO,
  historyInfo: historyInfo,
})

export const receiveGeneralInfo = generalInfo => ({
  type: X1_FNC_GENERALINFO,
  generalInfo: generalInfo,
})

export const receiveNodeStatus = nodeStatus => ({
  type: X1_FNC_NODESTATUS,
  nodeStatus: nodeStatus,
})
export const receiveWalletInfo = walletInfo => ({
  type: X1_RECEIVE_WALLETINFO,
  walletInfo: walletInfo,
})
export const resetFullNodeConnection = () => {
  return {
    type: X1_FNC_RESET,
  }
}

/**
 * showXActivityModal - Show the activity modal with a given activity item.
 *
 * @param {string} itemType Item type
 * @param {string} itemId Item id
 * @returns {Function} Thunk
 */
export const showXActivityModal = (item) => dispatch => {
  dispatch({ type: X1_SHOW_ACTIVITY_MODAL, historySelectedItem: item })
  dispatch(openModal('XACTIVITY_MODAL'))
}

export const toggleCreateNewWallet = () => ({
  type: X1_FNC_TOGGLE_CREATENEWWALLET,
})

export const setNewWalletPassphrase = fullNodeNewWalletPassphrase => ({
  type: X1_FNC_SETNEWWALLETPASSPHRASE,
  fullNodeNewWalletPassphrase: fullNodeNewWalletPassphrase,
})

export const setNewWalletName = fullNodeNewWalletName => ({
  type: X1_FNC_SETNEWWALLETNAME,
  fullNodeNewWalletName: fullNodeNewWalletName,
})

export const getWalletFiles = arg => dispatch => {
  dispatch(startGetWalletFiles())
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      return dispatch(endGetWalletFiles(responsePayload.responsePayload, true, true))
    } else if (responsePayload.status === 401) {
      return dispatch(endGetWalletFiles({}, true, false))
    } else {
      return dispatch(endGetWalletFiles({}, false, false))
    }
  })
}

export const callCreateWallet = () => (dispatch, getState) => {
  const { fullNodeNewWalletName, fullNodeNewWalletPassphrase } = getState().fullNodeConnection
  const arg = createRequestForWalletInStore('createWallet', {
    name: fullNodeNewWalletName,
    password: fullNodeNewWalletPassphrase,
  })
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(walletCreateResult(true))
    } else {
      dispatch(walletCreateResult(false))
    }
  })
}

export const saveWallet = data => dispatch => {
  let ser = JSON.stringify(data)

  let wallet = {
    type: 'fullNodeWallet',
    chain: 'obsidianx',
    network: 'mainnet',
    autopilot: false,
    alias: ser,
    name: data.fullNodeSelectedWalletName,
  }

  let { id: walletId } = dispatch(putWallet(wallet))
}

const regExIP46DNSHost = new RegExp(
  '^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]).){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$|^(([a-zA-Z]|[a-zA-Z][a-zA-Z0-9-]*[a-zA-Z0-9]).)*([A-Za-z]|[A-Za-z][A-Za-z0-9-]*[A-Za-z0-9])$|^(?:(?:(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):){6})(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):(?:(?:[0-9a-fA-F]{1,4})))|(?:(?:(?:(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])).){3}(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])))))))|(?:(?:::(?:(?:(?:[0-9a-fA-F]{1,4})):){5})(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):(?:(?:[0-9a-fA-F]{1,4})))|(?:(?:(?:(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])).){3}(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])))))))|(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})))?::(?:(?:(?:[0-9a-fA-F]{1,4})):){4})(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):(?:(?:[0-9a-fA-F]{1,4})))|(?:(?:(?:(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])).){3}(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])))))))|(?:(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):){0,1}(?:(?:[0-9a-fA-F]{1,4})))?::(?:(?:(?:[0-9a-fA-F]{1,4})):){3})(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):(?:(?:[0-9a-fA-F]{1,4})))|(?:(?:(?:(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])).){3}(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])))))))|(?:(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):){0,2}(?:(?:[0-9a-fA-F]{1,4})))?::(?:(?:(?:[0-9a-fA-F]{1,4})):){2})(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):(?:(?:[0-9a-fA-F]{1,4})))|(?:(?:(?:(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])).){3}(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])))))))|(?:(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):){0,3}(?:(?:[0-9a-fA-F]{1,4})))?::(?:(?:[0-9a-fA-F]{1,4})):)(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):(?:(?:[0-9a-fA-F]{1,4})))|(?:(?:(?:(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])).){3}(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])))))))|(?:(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):){0,4}(?:(?:[0-9a-fA-F]{1,4})))?::)(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):(?:(?:[0-9a-fA-F]{1,4})))|(?:(?:(?:(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])).){3}(?:(?:25[0-5]|(?:[1-9]|1[0-9]|2[0-4])?[0-9])))))))|(?:(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):){0,5}(?:(?:[0-9a-fA-F]{1,4})))?::)(?:(?:[0-9a-fA-F]{1,4})))|(?:(?:(?:(?:(?:(?:[0-9a-fA-F]{1,4})):){0,6}(?:(?:[0-9a-fA-F]{1,4})))?::))))$'
)

export const getFullNodeKey = () => (dispatch, getState) => {
  const {
    fullNodeHost,
    fullNodeIsConnectionSuccess,
    fullNodeActualAuthKey,
    fullNodeIsLoading,
  } = getState().fullNodeConnection

  if (fullNodeIsLoading) {
    return
  }

  if (fullNodeHost && regExIP46DNSHost.test(fullNodeHost)) {
    if (fullNodeIsConnectionSuccess && fullNodeActualAuthKey) {
      return
    }
    const requestObject = new RequestObject('getKey', '')
    requestObject.ipAddress = fullNodeHost
    return ApiService.makeRequest(requestObject).then(responsePayload => {
      if (responsePayload.status === 200) {
        console.log('getKey request successful, received authKey: ' + responsePayload.authKey)
        dispatch(setHost(fullNodeHost, true, responsePayload.authKey))
      } else {
        dispatch(setHost(fullNodeHost, false, ''))
      }
    })
  }

  dispatch(setErrorText('regEx: Invalid or missing IP/Host'))
  dispatch(setHost(fullNodeHost, false, ''))
}

export const testHost = () => (dispatch, getState) => {
  // reset data in case the host changed
  dispatch(setIsCredentialsRequired(null))
  dispatch(setIsCredentialsSuccess(null))
  dispatch(receiveDaemonInfo())

  const requestObject = new RequestObject('getKey', '')
  requestObject.ipAddress = getState().fullNodeConnection.fullNodeHost
  ApiService.makeRequest(requestObject).then(responsePayload => {
    if (responsePayload.status === 200 && responsePayload.authKey) {
      dispatch(setHost(requestObject.ipAddress, true, responsePayload.authKey))

      // the connection was successful, now test if the host requires credentials,
      // by trying access without any credentials
      const test = new RequestObject('daemonInfo', '')
      test.ipAddress = requestObject.ipAddress
      test.expectedAuthKey = responsePayload.authKey
      ApiService.makeRequest(test).then(responsePayload => {
        if (responsePayload.status === 200) {
          // host doesn't require credentials, we have access
          dispatch(setIsCredentialsRequired(false))
          // and save the wallet files, since we have them
          dispatch(
            receiveDaemonInfo(
              responsePayload.responsePayload
            )
          )
        } else if (responsePayload.status === 401) {
          // host explicitly requires credentials
          dispatch(setIsCredentialsRequired(true))
        } else {
          // something went wrong, this should be interpreted as unsuccessful connection
          dispatch(setIsCredentialsRequired(null))
        }
      })
    } else {
      dispatch(setHost(requestObject.ipAddress, false, ''))
      dispatch(setIsCredentialsRequired(null))
    }
  })
}

export const testCredentials = () => (dispatch, getState) => {
  // the credentials to use are expected to be in the store
  const requestObject = createRequestForWalletInStore('daemonInfo')
  ApiService.makeRequest(requestObject).then(responsePayload => {
    if (responsePayload.status === 200) {
      // the credentials were correct
      dispatch(setIsCredentialsSuccess(true))
      // and save the wallet files, since we have them
      dispatch(
        receiveDaemonInfo(
          responsePayload.responsePayload
        )
      )
    } else if (responsePayload.status === 401) {
      // the credentials were not correct
      dispatch(setIsCredentialsSuccess(false))
      dispatch(receiveDaemonInfo())
    } else {
      // something went wrong, this should be interpreted as unsuccessful connection
      dispatch(setIsCredentialsSuccess(null))
      dispatch(receiveDaemonInfo())
    }
  })
}


export const callLoadWallet = fullNodeWallet => dispatch => {
  const requestObject = new RequestObject('getKey', '')
  requestObject.ipAddress = fullNodeWallet.fullNodeHost
  return ApiService.makeRequest(requestObject).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(setHost(requestObject.ipAddress, true, responsePayload.authKey))
      const arg = createRequestForWallet('loadWallet', '', fullNodeWallet)
      return ApiService.makeRequest(arg).then(responsePayload => {
        if (responsePayload.status === 200) {
          dispatch(setPassphraseChallenge(responsePayload.responsePayload.passphraseChallenge))
        }
      })
    } else {
      dispatch(setHost(requestObject.ipAddress, false, ''))
    }
  })
}

export const callUnlockWallet = () => async (dispatch, getState) => {
  const { passphrase } = getState().fullNodePassphrase
  const arg = createRequestForWalletInStore('startStaking', { passphrase: passphrase })
  dispatch(clearPassphrase())
  const responsePayload = await ApiService.makeRequest(arg)
  if (responsePayload.status === 200) {
    dispatch(callWalletInfo())
  }
}

export const callStopStaking = () => dispatch => {
  const arg = createRequestForWalletInStore('stopStaking', '')
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      callWalletInfo()
    }
  })
}

export const callWalletInfo = () => dispatch => {
  const arg = createRequestForWalletInStore('walletInfo', '')
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(receiveWalletInfo(responsePayload.responsePayload))
    }
  })
}

export const callDaemonInfo = () => dispatch => {
  const arg = createRequestForWalletInStore('daemonInfo', '')
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(receiveDaemonInfo(responsePayload.responsePayload))
    }
  })
}

export const callGeneralInfo = () => dispatch => {
  const arg = createRequestForWalletInStore('generalInfo', '')
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      dispatch(receiveGeneralInfo(responsePayload.responsePayload))
      const {
        chainTip,
        connectedNode,
        creationTime,
        isChainSynced,
        isDecrypted,
        lastBlockSyncedHeight,
        network,
        walletFilePath,
      } = responsePayload.responsePayload
      /*
  alias: "03e47dfed287b392c4fa"
  best_header_timestamp: 1564942444
  block_hash: "0000000000000000000762a926ea78680d1f033b5cf4da9bad5226288d6535a7"
  block_height: 588604
  chains: [{…}]
  color: "#3399ff"
  grpcProtoVersion: "0.7.1-beta"
  identity_pubkey: "03e47dfed287b392c4fa6a886d33952a7b0e813c821cab0653cd172f1da1b7cc87"
  num_active_channels: 0
  num_inactive_channels: 0
  num_peers: 2
  num_pending_channels: 0
  synced_to_chain: true
  testnet: false
  uris: []
  version:
*/

      const info = {
        chains: [{ chain: 'obsidianx', network: 'mainnet' }], // chain 'bitcoin' sets BTC units, network 'mainnet'/'testnet' displays a 't' before the unit, when 'testnet'
        testnet: false, // ?!
        synced_to_chain: isChainSynced, // true displays a greem light next to the wallet name
        block_height: chainTip,
        color: '#FF00ff', // ?!
      }

      dispatch(setInfo(info))
    }
  })
}

export const callBalance = () => dispatch => {
  const arg = createRequestForWalletInStore('balance', '')
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      const {
        amountConfirmed,
        amountUnconfirmed,
        spendableAmount,
      } = responsePayload.responsePayload
      dispatch(
        receiveBalance(amountConfirmed.satoshi, amountUnconfirmed.satoshi, spendableAmount.satoshi)
      )
    }
  })
}

export const callHistoryInfo = () => dispatch => {
  const arg = createRequestForWalletInStore('historyInfo', {
    skip: null,
    take: 500,
    q: '',
  })
  return ApiService.makeRequest(arg).then(responsePayload => {
    if (responsePayload.status === 200) {
      /*const { history } = responsePayload.responsePayload
      const { transactionsHistory } = history[0]

      let activityArr = []
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

        if (type !== 'received' && type !== 'staked' && type !== 'send') {
          console.log('Unknown transaction type detected: ' + type ? type : 'falsy')
        }

        activityArr[i] = {
          type: 'transaction', // transaction, payment, invoice
          time_stamp: timestamp,
          tx_hash: id,
          amount: amount.satoshi,
          sending: false,
          status: 'successful',
          error: '',
          received: type === 'received' || type === 'staked' ? true : false, // true, dalse
        }
      }*/
      dispatch(receiveHistoryInfo(responsePayload.responsePayload))
    }
  })
}

export const loadWalletIntoStore = fullNodeWallet => dispatch => {
  dispatch(resetFullNodeConnection())
  const {
    fullNodeHost,
    fullNodeExpectedAuthKey,
    fullNodeUser,
    fullNodePwd,
    fullNodeSelectedWalletName,
  } = fullNodeWallet
  dispatch(setHost(fullNodeHost, false, ''))
  dispatch(setUser(fullNodeUser, false))
  dispatch(setPwd(fullNodePwd, false))
  dispatch(setExpectedAuthKey(fullNodeExpectedAuthKey))
  dispatch(selectWallet(fullNodeSelectedWalletName))
}

const createRequestForWalletInStore = (command, payload = '') => {
  const request = new RequestObject(command, payload)
  const {
    fullNodeHost,
    fullNodeExpectedAuthKey,
    fullNodeUser,
    fullNodePwd,
    fullNodeSelectedWalletName,
  } = x1Store.getState().fullNodeConnection
  request.target = fullNodeSelectedWalletName
  request.ipAddress = fullNodeHost
  request.user = fullNodeUser
  request.password = fullNodePwd
  request.expectedAuthKey = fullNodeExpectedAuthKey
  return request
}

const createRequestForWallet = (command, payload, fullNodeWallet) => {
  const request = new RequestObject(command, payload)
  const {
    fullNodeHost,
    fullNodeExpectedAuthKey,
    fullNodeUser,
    fullNodePwd,
    fullNodeSelectedWalletName,
  } = fullNodeWallet
  request.target = fullNodeSelectedWalletName
  request.ipAddress = fullNodeHost
  request.user = fullNodeUser
  request.password = fullNodePwd
  request.expectedAuthKey = fullNodeExpectedAuthKey
  return request
}
