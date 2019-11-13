/** Reducers 
https://redux.js.org/basics/reducers
Reducers specify how the application's state changes in response to actions sent to the store. 
Remember that actions only describe what happened, but don't describe how the application's state changes.

The reducer is a pure function that takes the previous state and an action, and returns the next state.

It's very important that the reducer stays pure. Things you should never do inside a reducer:
- Mutate its arguments;
- Perform side effects like API calls and routing transitions;
- Call non-pure functions, e.g. Date.now() or Math.random().
 */

import {
  X1_FNC_SETHOST,
  X1_FNC_SETERRORTEXT,
  X1_FNC_SETEXPECTEDAUTHKEY,
  X1_FNC_SETCREDENTIALSREQUIRED,
  X1_FNC_SETUSER,
  X1_FNC_SETPWD,
  X1_FNC_SETCREDENTIALSSUCCESS,
  X1_RECEIVE_DAEMONINFO,
  X1_FNC_SETWALLETNAME,
  X1_FNC_RESET,
  X1_RECEIVE_TRANSACTIONRESPONSE,
  X1_FNC_SETCRYPTOUNIT,
  X1_RECEIVE_HISTORYINFO,
  X1_FNC_SETISLOADING,
  X1_FNC_TOGGLE_CREATENEWWALLET,
  X1_FNC_SETNEWWALLETNAME,
  X1_FNC_SETNEWWALLETPASSPHRASE,
  X1_FNC_SETWALLETCREATERESULT,
  X1_SHOW_ACTIVITY_MODAL,
} from './fullNodeConnectionActions'

/* Redux will call our reducer with an undefined state for the first time. This is our chance to return the initial state of our app: 
    if (typeof state === "undefined") 
      return initialState
*/
const initialState = {
  // persistance
  fullNodeHost: '127.0.0.1',
  fullNodeExpectedAuthKey: '',
  fullNodeUser: '',
  fullNodePwd: '',
  fullNodeWalletFilePath: '',
  fullNodeSelectedWalletName: '',

  // no persistance
  fullNodeIsLoading: false, // false | true
  fullNodeIsLoadingCommand: '', // string - requestObject.command at the beginng of the request, '' when finished

  fullNodeActualAuthKey: '',
  fullNodeIsConnectionSuccess: null, // null | false | true
  fullNodeConnectionError: '',
  fullNodeIsCredentialsRequired: null, // null | false | true
  fullNodeIsCredentialsSuccess: null, // null | false | true
  daemonInfo: {
    agentName: '', // e.g. 'x1d 1.1.7244 (Debug)'
    assemblyVersion: '', // e.g. '1.1.7244'
    codeBase: '', // e.g. 'C:/Users/[USER]/source/repos/netsfx/stratis/Obsidian-StratisNode/src/Obsidian.x1d/bin/Debug/netcoreapp3.0/x1d.dll'
    coinTicker: '', // e.g. 'ODX'
    features: [{ namedItem: 'n/a' }], // e.g.
    machineName: '', // e.g. 'MyMac'
    minTxFee: 0, // e.g. 100000
    minTxRelayFee: 0, // e.g. 100000
    networkName: '', // e.g. 'ObsidianXMain'
    processId: 0, // e.g. 24984
    processMemory: 0, // e.g. 112123904
    processName: '', // e.g. 'x1d'
    startupTime: 0, // e.g. 1572603177
    testnet: false, // e.g. false
    walletFiles: [{ namedItem: 'n/a' }], // e.g.
    walletPath: '', // a path string
  },
  historySelectedItem: {},
  historyInfo: {
    blocks: [
      /* {
        height: 5835,
        time: 1572628544,
        hashBlock: '1a0d788490a2a87ca594b919b43488b1cc5447114520bc457433dea2d0234352',
        transactions: [
          {
            totalSpent: 210000000000,
            totalReceived: 215000000000,
            valueAdded: 5000000000,
            txType: 'Coinstake',
            hashTx: '9eff5b15a675de631f27e7ab9c8e1ca71d8aebb0d5f5089e93862d107d812d0d',
          },
        ],
      },*/
    ],
  },

  fullNodeCreateNewWallet: false,
  fullNodeNewWalletName: '',
  fullNodeNewWalletPassphrase: '',
  fullNodeWalletCreateSuccess: false,
}

export default function fullNodeConnection(state = initialState, action) {
  switch (action.type) {
    case X1_FNC_SETISLOADING:
      return Object.assign({}, state, {
        fullNodeIsLoading: action.isLoading,
        fullNodeIsLoadingCommand: action.command,
      })
    case X1_FNC_SETHOST:
      /* The Object.assign() method is used to copy the values of all enumerable own properties from one or more source objects
      to a target object and returns the target object: Object.assign(target, source1, source2, ...source n)
      We don't mutate the state, therefore we supply a new empty object as the first parameter. 
      */
      return Object.assign({}, state, {
        fullNodeHost: action.ipAddress,
        fullNodeIsConnectionSuccess: action.fullNodeIsConnectionSuccess,
        fullNodeActualAuthKey: action.authKey,
      })
    case X1_FNC_SETERRORTEXT:
      return Object.assign({}, state, {
        fullNodeConnectionError: action.errorText,
      })
    case X1_FNC_SETEXPECTEDAUTHKEY:
      return Object.assign({}, state, {
        fullNodeExpectedAuthKey: action.authKey,
      })
    case X1_FNC_SETCREDENTIALSREQUIRED:
      return Object.assign({}, state, {
        fullNodeIsCredentialsRequired: action.isCredentialsRequired,
      })
    case X1_FNC_SETUSER:
      return Object.assign({}, state, {
        fullNodeUser: action.user,
      })
    case X1_FNC_SETPWD:
      return Object.assign({}, state, {
        fullNodePwd: action.pwd,
      })
    case X1_FNC_SETCREDENTIALSSUCCESS:
      return Object.assign({}, state, {
        fullNodeIsCredentialsSuccess: action.isSuccess,
      })

    case X1_FNC_SETWALLETNAME:
      return Object.assign({}, state, {
        fullNodeSelectedWalletName: action.fullNodeSelectedWalletName,
      })
    case X1_FNC_TOGGLE_CREATENEWWALLET:
      return Object.assign({}, state, {
        fullNodeCreateNewWallet: !state.fullNodeCreateNewWallet,
      })
    case X1_FNC_SETNEWWALLETNAME:
      return Object.assign({}, state, {
        fullNodeNewWalletName: action.fullNodeNewWalletName,
      })
    case X1_FNC_SETNEWWALLETPASSPHRASE:
      return Object.assign({}, state, {
        fullNodeNewWalletPassphrase: action.fullNodeNewWalletPassphrase,
      })
    case X1_FNC_SETWALLETCREATERESULT:
      return Object.assign({}, state, {
        fullNodeWalletCreateSuccess: action.fullNodeWalletCreateSuccess,
      })
    case X1_FNC_RESET:
      return Object.assign({}, initialState)

    case X1_FNC_SETCRYPTOUNIT:
      return Object.assign({}, state, {
        cryptoUnit: action.cryptoUnit,
      })

    case X1_RECEIVE_DAEMONINFO:
      if (!action.daemonInfo) {
        action.daemonInfo = initialState.daemonInfo
      }
      return Object.assign({}, state, {
        daemonInfo: action.daemonInfo,
      })
      
    case X1_RECEIVE_HISTORYINFO:
      if (!action.historyInfo) {
        action.historyInfo = initialState.historyInfo
      }
      return Object.assign({}, state, {
        historyInfo: action.historyInfo,
      })
      case X1_SHOW_ACTIVITY_MODAL:
          return Object.assign({}, state, {
            historySelectedItem: action.historySelectedItem,
          })
    default:
      return state
  }
}
