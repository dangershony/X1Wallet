import React from 'react'
import PropTypes from 'prop-types'
import { connect } from 'react-redux'
import { FormattedMessage, injectIntl } from 'react-intl'
import { Box, Flex } from 'rebass'
import { Bar, Form, DataRow, Input, Label, Text, Header } from 'components/UI'
import {
  saveWallet,
  resetFullNodeConnection,
  callCreateWallet,
} from './reducers/fullNodeConnectionActions'
import { Redirect } from 'react-router-dom'
import messages from '../renderer/components/Home/messages'
import x1Messages from './x1Xessages'
import ConnectionDetailsTabs from './X1ConnectionDetailsTabs'

class X1ConnectionConfirm extends React.Component {
  static propTypes = {
    wizardApi: PropTypes.object,
    wizardState: PropTypes.object,
  }

  static defaultProps = {
    wizardApi: {},
    wizardState: {},
  }

  componentDidUpdate(prevProps) {
    /*
    const { wizardApi, lndConnect } = this.props
    if (lndConnect && lndConnect !== prevProps.lndConnect) {
      wizardApi.navigateTo(0)
    }*/
  }

  isSubmitted = false

  handleSubmit = async () => {
    let isValid
    const {
      fullNodeHost,
      fullNodeUser,
      fullNodePwd,
      fullNodeIsConnectionSuccess,
      fullNodeIsCredentialsSuccess,
      fullNodeActualAuthKey,
      fullNodeExpectedAuthKey,
      fullNodeSelectedWalletName,
      fullNodeWalletFilePath,
      fullNodeNewWalletName,
      fullNodeNewWalletPassphrase,
      fullNodeCreateNewWallet,
    } = this.props.fullNodeConnection

    if (
      fullNodeHost &&
      fullNodeIsConnectionSuccess &&
      fullNodeIsCredentialsSuccess &&
      fullNodeActualAuthKey &&
      fullNodeActualAuthKey === fullNodeExpectedAuthKey &&
      (fullNodeSelectedWalletName || fullNodeNewWalletName)
    ) {
      isValid = true
    }

    if (!isValid) {
      this.props.wizardApi.previous()
    }
    if (fullNodeCreateNewWallet || !fullNodeSelectedWalletName) {
      if (fullNodeNewWalletName && fullNodeNewWalletPassphrase) {
        x1Store.dispatch(callCreateWallet())
      } else {
        this.props.wizardApi.previous()
      }
    }

    x1Store.dispatch(
      saveWallet({
        fullNodeHost: fullNodeHost,
        fullNodeExpectedAuthKey: fullNodeExpectedAuthKey,
        fullNodeUser: fullNodeUser,
        fullNodePwd: fullNodePwd,
        fullNodeWalletFilePath: fullNodeWalletFilePath,
        fullNodeSelectedWalletName:
          fullNodeCreateNewWallet || !fullNodeSelectedWalletName
            ? fullNodeNewWalletName + '.' + this.props.daemonInfo.coinTicker + '.x1wallet.json'
            : fullNodeSelectedWalletName,
      })
    )
    this.isSubmitted = true
    x1Store.dispatch(resetFullNodeConnection())
  }

  render() {
    const { wizardApi, wizardState, ...rest } = this.props

    const {
      fullNodeHost,
      fullNodeActualAuthKey,
      fullNodeUser,
      fullNodePwd,
      fullNodeWalletFilePath,
      fullNodeSelectedWalletName,
      fullNodeNewWalletName,
      fullNodeCreateNewWallet,
      daemonInfo,
    } = this.props.fullNodeConnection

    const wallet = {
      fullNodeHost: fullNodeHost,
      fullNodeExpectedAuthKey: fullNodeActualAuthKey,
      fullNodeUser: fullNodeUser,
      fullNodePwd: fullNodePwd,
      fullNodeWalletFilePath: fullNodeWalletFilePath,
      fullNodeSelectedWalletName:
        fullNodeCreateNewWallet || !fullNodeSelectedWalletName
          ? fullNodeNewWalletName + '.' + daemonInfo.coinTicker + '.x1wallet.json'
          : fullNodeSelectedWalletName,
    }

    const { getApi, onSubmit, onSubmitFailure } = wizardApi
    return (
      <Form
        {...rest}
        getApi={getApi}
        onSubmit={async values => {
          try {
            await this.handleSubmit(values)

            if (onSubmit && !isLightningGrpcActive) {
              onSubmit(values)
            }
          } catch (e) {
            wizardApi.onSubmitFailure()
            wizardApi.previous()
          }
        }}
        onSubmitFailure={onSubmitFailure}
      >
        <Header subtitle="If everything is correct we'll save this." title="Confirm Settings" />
        <Bar mb={5} mt={4} />
        {this.isSubmitted ? (
          <Redirect to="/home" />
        ) : (
          <React.Fragment>
            <>
              <Text fontWeight="normal">Full Node Connection</Text>
              <Bar mb={4} mt={2} />

              <DataRow
                left={<FormattedMessage {...messages.host} />}
                py={2}
                right={wallet.fullNodeHost}
              />

              <DataRow
                left={<FormattedMessage {...messages.wallet_settings_name_label} />}
                py={2}
                right={wallet.fullNodeSelectedWalletName}
              />

              <DataRow
                left={
                  <>
                    <Label htmlFor="fullNodeUser" mb={2}>
                      API User
                    </Label>
                  </>
                }
                py={2}
                right={wallet.fullNodeUser}
              />

              <DataRow
                left={
                  <>
                    <Label htmlFor="fullNodePwd" mb={2}>
                      API Password
                    </Label>
                  </>
                }
                py={2}
                mb={4}
                right={'not shown'}
              />
            </>
            <Text fontWeight="normal">Auth Key</Text>
            <Bar mb={2} mt={2} />
            <Text>{wallet.fullNodeExpectedAuthKey}</Text>
          </React.Fragment>
        )}
      </Form>
    )
  }
}

const mapStateToProps = state => ({
  fullNodeConnection: state.fullNodeConnection,
})

export default injectIntl(connect(mapStateToProps)(X1ConnectionConfirm))
