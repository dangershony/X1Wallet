import React, { useEffect } from 'react'
import PropTypes from 'prop-types'
import { Flex } from 'rebass'
import XWalletContainer from '../XWallet/XWalletContainer'
import XActivityContainer from '../XActivity/XActivityContainer'

// Bitcoin blocks come on average every 10 mins
// but we poll a lot more frequently to make UI a little bit more responsive
const TX_REFETCH_INTERVAL = 1000 * 60 * 1

// Refresh autopilot scores every hour.
const AUTOPILOT_SCORES_REFRESH_INTERVAL = 1000 * 60 * 60

// Initial re-fetch after 30 seconds.
const PEERS_INITIAL_REFETCH_INTERVAL = 1000 * 30

// Fetch peers list data no less than once every 10 minutes.
const PEERS_MAX_REFETCH_INTERVAL = 1000 * 60 * 10

// Amount to increment re-fetch timer by after each fetch.
const PEERS_REFETCH_BACKOFF_SCHEDULE = 2

const XAppComponent = ({
  modals,
  redirectPayReq,
  updateAutopilotNodeScores,
  xfetchActivityHistory,
  setIsWalletOpen,
  fetchPeers,
  setModals,
  initBackupService,
  fetchSuggestedNodes,
  initTickers,
  fullNodeConnection,
  callHistoryInfo,
  callGeneralInfo,
  callWalletInfo,
  callDaemonInfo,
}) => {
  /**
   * App scheduler / polling service setup. Add new app-wide polls here
   *
   * Fetch node data on an exponentially incrementing backoff schedule so that when the app is first mounted, we fetch
   * node data quite frequently but as time goes on the frequency is reduced to a max of PEERS_MAX_REFETCH_INTERVAL
   *
   * useEffect: see https://reactjs.org/docs/hooks-reference.html#useeffect
   **/

  useEffect(() => {
    window.x1Tools.scheduler.addTask({
      task: callWalletInfo,
      taskId: 'callWalletInfo',
      baseDelay: 200,
      maxDelay: 1000 * 15, 
      backoff: 10
    })

    window.x1Tools.scheduler.addTask({
      task: callHistoryInfo,
      taskId: 'callHistoryInfo',
      baseDelay: 200,
      maxDelay: 1000 * 15, 
      backoff: 10
    })

    window.x1Tools.scheduler.addTask({
      task: callDaemonInfo,
      taskId: 'callDaemonInfo',
      baseDelay: 1000 * 60,
    })

    // clean up - this function will run when the component unmounts or re-renders the Effect (which we avoid, see below)
    return () => {
      window.x1Tools.scheduler.removeTask('callWalletInfo')
      window.x1Tools.scheduler.removeTask('callHistoryInfo')
      window.x1Tools.scheduler.removeTask('callDaemonInfo')
    }
  }, [callWalletInfo, callHistoryInfo, callDaemonInfo]) // do not re-run on re-render, if the functions have not changed

  useEffect(() => {
    // Set wallet open state.
    setIsWalletOpen(true)
    // fetch balance and transactions
    callWalletInfo()
    callHistoryInfo()
    callDaemonInfo()

    initTickers()
  }, [setIsWalletOpen, callWalletInfo, callHistoryInfo, callDaemonInfo, initTickers])

  // Open the pay form when a payment link is used.
  useEffect(() => {
    if (redirectPayReq) {
      if (!modals.find(m => m.type === 'PAY_FORM')) {
        setModals([{ type: 'PAY_FORM' }])
      }
    }
  }, [redirectPayReq, modals, setModals])

  return (
    <Flex as="article" flexDirection="column" width={1}>
      <XWalletContainer />
      <XActivityContainer />
    </Flex>
  )
}

XAppComponent.propTypes = {
  callWalletInfo: PropTypes.func.isRequired,
  callHistoryInfo: PropTypes.func.isRequired,
  callDaemonInfo: PropTypes.func.isRequired,

  initTickers: PropTypes.func.isRequired,
  modals: PropTypes.array.isRequired,
  redirectPayReq: PropTypes.object,
  setIsWalletOpen: PropTypes.func.isRequired,
  // setModals: PropTypes.func.isRequired,
  // updateAutopilotNodeScores: PropTypes.func.isRequired,
  fullNodeConnection: PropTypes.object.isRequired,
}

export default XAppComponent
