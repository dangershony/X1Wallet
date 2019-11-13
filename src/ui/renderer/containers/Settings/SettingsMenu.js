import { connect } from 'react-redux'
import { setLocale } from 'reducers/locale'
import { setFiatTicker, tickerSelectors } from 'reducers/ticker'
import { infoSelectors } from 'reducers/info'
import { openSettingsMenu, closeSettingsMenu } from 'reducers/settingsmenu'
import { setTheme, themeSelectors } from 'reducers/theme'
import { walletSelectors } from 'reducers/wallet'
import { openModal } from 'reducers/modal'
import SettingsMenu from 'components/Settings/SettingsMenu'

const mapStateToProps = state => ({
  activeSubMenu: state.settingsmenu.activeSubMenu,
  activeWalletSettings: walletSelectors.activeWalletSettings(state),
  fiatTicker: tickerSelectors.fiatTicker(state),
  fiatTickers: tickerSelectors.fiatTickers(state),
  locales: state.locale,
  currentLocale: state.intl.locale,
  themes: state.theme.themes,
  currentTheme: themeSelectors.currentTheme(state),
  isSettingsMenuOpen: state.settingsmenu.isSettingsMenuOpen,
  isWalletReady: window.x1Tools.isZap() ? infoSelectors.isSyncedToChain(state) : getIsSynced(state),
})

const getIsSynced = state => {
  return (
    state.fullNodeSettings.walletInfo &&
    state.fullNodeSettings.walletInfo.consensusTipHash &&
    state.fullNodeSettings.walletInfo.consensusTipHeight > 0 &&
    state.fullNodeSettings.walletInfo.walletDetails &&
    state.fullNodeSettings.walletInfo.consensusTipHash ===
      state.fullNodeSettings.walletInfo.walletDetails.syncedHash
  )
}

const mapDispatchToProps = {
  openSettingsMenu,
  openModal,
  closeSettingsMenu,
  setFiatTicker,
  setLocale,
  setTheme,
}

const mergeProps = (stateProps, dispatchProps, ownProps) => ({
  activeWalletSettings: stateProps.activeWalletSettings,
  isWalletReady: stateProps.isWalletReady,
  activeSubMenu: stateProps.activeSubMenu,
  isSettingsMenuOpen: stateProps.isSettingsMenuOpen,
  openModal: dispatchProps.openModal,
  openSettingsMenu: dispatchProps.openSettingsMenu,
  closeSettingsMenu: dispatchProps.closeSettingsMenu,
  ...ownProps,

  fiatProps: {
    fiatTicker: stateProps.fiatTicker,
    fiatTickers: stateProps.fiatTickers,
    setFiatTicker: dispatchProps.setFiatTicker,
  },

  localeProps: {
    locales: stateProps.locales,
    currentLocale: stateProps.currentLocale,
    setLocale: dispatchProps.setLocale,
  },

  themeProps: {
    themes: stateProps.themes,
    currentTheme: stateProps.currentTheme,
    setTheme: dispatchProps.setTheme,
  },
})

export default connect(
  mapStateToProps,
  mapDispatchToProps,
  mergeProps
)(SettingsMenu)
