import React from 'react'
import { connect } from 'react-redux'
import PropTypes from 'prop-types'
import { FormattedMessage, injectIntl } from 'react-intl'
import { Box, Flex } from 'rebass'
import {
  Bar,
  Button,
  Label,
  Form,
  Header,
  Input,
  OpenDialogInput,
  Spinner,
  Text,
  DataRow,
  Select,
  FieldLabel,
} from 'components/UI'
import ConnectionDetailsTabs from './X1ConnectionDetailsTabs'
import {
  setHost,
  setUser,
  setPwd,
  selectWallet,
  resetFullNodeConnection,
  setExpectedAuthKey,
  toggleCreateNewWallet,
  setNewWalletPassphrase,
  setNewWalletName,
  testHost,
  testCredentials,
} from './reducers/fullNodeConnectionActions'

class X1ConnectionDetailsSecureApi extends React.Component {
  static propTypes = {
    wizardApi: PropTypes.object,
    wizardState: PropTypes.object,
  }

  static defaultProps = {
    wizardApi: {},
    wizardState: {},
  }

  componentDidMount() {
    /*
    const {
      fullNodeHost,
      fullNodeIsConnectionSuccess,
      fullNodeIsCredentialsSuccess,
      fullNodeIsLoading,
      fullNodePwd,
      fullNodeSelectedWalletName,
      fullNodeUser,
    } = this.props.fullNodeConnection


    // If at least one of the fields has an error, set them all as touched so that they get highlighted.
    if (!fullNodeIsConnectionSuccess || !fullNodeIsCredentialsSuccess) {
      this.formApi.setTouched('fullNodeHost', true)
      this.formApi.setTouched('fullNodeUser', true)
      this.formApi.setTouched('fullNodePwd', true)
    }

    // If we have a connection error, set it into the form errors for the relevant field.
    if (!fullNodeIsConnectionSuccess) {
      this.formApi.setError('fullNodeHost', 'fullNodeHost')
    }
    if (!fullNodeIsCredentialsSuccess) {
      this.formApi.setError('fullNodeUser', 'fullNodeUser')
      this.formApi.setError('fullNodePwd', 'fullNodePwd')
    }
  */
  }

  asyncValidateField = async (field, validator) => {
    const value = this.formApi.getValue(field)
    if (!value) {
      return
    }

    const validatorWrapper = async () => {
      try {
        await validator(value)
      } catch (e) {
        return e.toString()
      }
    }

    const result = await validatorWrapper(field, validator)
    if (result === true) {
      this.formApi.setError(field, undefined)
    } else {
      this.formApi.setError(field, result)
    }
  }

  handleSubmit = values => {}

  validateHost = () => {
    const { fullNodeHost } = this.props.fullNodeConnection
    return this.asyncValidateField(
      'fullNodeHost',
      (function(arg) {
        if (fullNodeHost) {
          return true
        }
        return false
      })(fullNodeHost)
    )
  }

  validateFullNodeSelectedWalletName = () => {
    const { fullNodeSelectedWalletName } = this.props.fullNodeConnection
    return this.asyncValidateField('fullNodeSelectedWalletName', function() {
      return true
    })
  }

  setFormApi = formApi => {
    this.formApi = formApi
  }

  handleIpChange = e => {
    x1Store.dispatch(setHost(e.target.value))
  }
  handleUserChange = e => {
    x1Store.dispatch(setUser(e.target.value))
  }
  handlePwdChange = e => {
    x1Store.dispatch(setPwd(e.target.value))
  }

  testConnection = e => {
    e.preventDefault()
    const {
      fullNodeIsConnectionSuccess,
      fullNodeIsCredentialsRequired,
    } = x1Store.getState().fullNodeConnection
    if (fullNodeIsConnectionSuccess === true && fullNodeIsCredentialsRequired === true) {
      window.x1Store.dispatch(testCredentials())
    } else {
      window.x1Store.dispatch(testHost())
    }
  }

  acceptHost = e => {
    e.preventDefault()
    const { fullNodeActualAuthKey } = x1Store.getState().fullNodeConnection
    x1Store.dispatch(setExpectedAuthKey(fullNodeActualAuthKey))
  }

  createNewWallet = e => {
    e.preventDefault()
    x1Store.dispatch(toggleCreateNewWallet())
  }

  fullNodeNewWalletNameChanged = e => {
    x1Store.dispatch(setNewWalletName(e.target.value))
  }

  fullNodeNewWalletPassphraseChanged = e => {
    x1Store.dispatch(setNewWalletPassphrase(e.target.value))
  }

  onSelectWalletChange = fullNodeSelectedWalletName => {
    x1Store.dispatch(selectWallet(fullNodeSelectedWalletName))
  }

  render() {
    const {
      fullNodeHost,
      fullNodeIsConnectionSuccess,
      fullNodeIsCredentialsRequired,
      fullNodeIsCredentialsSuccess,
      fullNodeIsLoading,
      fullNodePwd,
      fullNodeSelectedWalletName,
      fullNodeUser,

      daemonInfo,

      fullNodeActualAuthKey,
      fullNodeExpectedAuthKey,
      fullNodeCreateNewWallet,
      fullNodeNewWalletName,
      fullNodeNewWalletPassphrase,
    } = this.props.fullNodeConnection
    const { wizardApi, wizardState, ...rest } = this.props

    let selectListItems = daemonInfo.walletFiles.map(function(w) {
      return { value: w.namedItem, key: w.namedItem }
    })

    const { getApi, onChange, onSubmit, onSubmitFailure } = wizardApi
    const { currentItem } = wizardState
    let isHostAccepted = false
    if (
      fullNodeExpectedAuthKey &&
      fullNodeExpectedAuthKey.length > 0 &&
      fullNodeExpectedAuthKey === fullNodeActualAuthKey
    ) {
      isHostAccepted = true
    }
    console.log('isHostAccepted: ' + isHostAccepted.toString())
    return (
      <Form
        {...rest}
        asyncValidators={[] /*[this.validateHost, this.validateFullNodeSelectedWalletName] */}
        getApi={formApi => {
          this.setFormApi(formApi)
          if (getApi) {
            getApi(formApi)
          }
        }}
        onChange={onChange && (formState => onChange(formState, currentItem))}
        onSubmit={values => {
          this.handleSubmit(values)
          if (onSubmit) {
            onSubmit(values)
          }
        }}
        onSubmitFailure={onSubmitFailure}
      >
        {({ formState }) => {
          const shouldValidateInline = formState.submits > 0 || fullNodeSelectedWalletName

          return (
            <>
              <Header
                title="Node Connection"
                subtitle="Please configure the connection to your local or remote node."
              />
              <Bar mb={1} mt={4} />
              <ConnectionDetailsTabs mb={3} />

              {!fullNodeIsConnectionSuccess ? (
                /* show host input */
                <Input
                  description={'127.0.0.1 for a local node or a remote IP address.'}
                  field="fullNodeHost"
                  initialValue={fullNodeHost}
                  isRequired
                  label={'IP Address'}
                  mb={3}
                  name="fullNodeHost"
                  willAutoFocus
                  onChange={this.handleIpChange}
                  isDisabled={fullNodeIsLoading}
                />
              ) : !isHostAccepted ? (
                /* show accept key button */
                <div>
                  Connection to {fullNodeHost} successful!
                  <br />
                  The host's public key is:
                  <br />
                  {fullNodeActualAuthKey}
                  <br />
                  Do you want to trust this host?
                  <br />
                  <Button
                    onClick={this.acceptHost}
                    size="medium"
                    type="button"
                    isDisabled={fullNodeIsLoading}
                  >
                    Yes
                  </Button>
                </div>
              ) : (
                /* show accept key button */
                <div>
                  Connection to {fullNodeHost} successful!
                  <br />
                  You trust:
                  <br />
                  {fullNodeActualAuthKey}
                  <br />
                  {fullNodeUser && fullNodePwd && fullNodeIsConnectionSuccess ? (
                    <Text>Credentials: {fullNodeUser}, Password: accepted</Text>
                  ) : null}
                </div>
              )}

              {fullNodeIsConnectionSuccess && isHostAccepted && fullNodeIsCredentialsRequired ? (
                <div>
                  <h2>The node is configured to require a user name and a password</h2>
                  <Input
                    description={
                      'If you have configured a SecureApi user name, please enter it here. Otherwise leave this blank.'
                    }
                    field="fullNodeUser"
                    initialValue={fullNodeUser}
                    label={'Connection User'}
                    mb={3}
                    name="fullNodeUser"
                    onBlur={this.handleUserChange}
                    isDisabled={fullNodeIsLoading}
                  />
                  <Input
                    description={
                      'If you have configured a SecureApi password, please enter it here. Otherwise leave this blank.'
                    }
                    field="fullNodePwd"
                    initialValue={fullNodePwd}
                    label={'Connection Password'}
                    mb={3}
                    name="fullNodePwd"
                    onBlur={this.handlePwdChange}
                    isDisabled={fullNodeIsLoading}
                  />
                </div>
              ) : null}

              {fullNodeIsConnectionSuccess &&
              isHostAccepted &&
              ((fullNodeIsCredentialsSuccess === true && fullNodeIsCredentialsRequired === true) ||
                fullNodeIsCredentialsRequired === false) ? (
                <div>
                  {daemonInfo.walletFiles.length > 0 && !fullNodeCreateNewWallet ? (
                    <>
                      <DataRow
                        left={'Wallet'}
                        right={
                          <Select
                            field="fullNodeSelectedWalletName"
                            highlightOnValid={true}
                            items={selectListItems}
                            isRequired={true}
                            onValueSelected={this.onSelectWalletChange}
                          />
                        }
                      />
                      <Bar mb={1} mt={4} />
                      <Button
                        onClick={this.createNewWallet}
                        size="medium"
                        type="button"
                        isDisabled={fullNodeIsLoading}
                      >
                        Create new wallet
                      </Button>
                    </>
                  ) : null}

                  {fullNodeCreateNewWallet || daemonInfo.walletFiles.length < 1 ? (
                    <div>
                      <Input
                        description={'Please enter a friendly name for your new wallet.'}
                        field="fullNodeNewWalletName"
                        initialValue={fullNodeNewWalletName}
                        label={'Wallet name'}
                        mb={3}
                        name="fullNodeNewWalletName"
                        onBlur={this.fullNodeNewWalletNameChanged}
                        isDisabled={fullNodeIsLoading}
                      />
                      <Input
                        description={
                          'Please enter a passphrase for the encryption of your private keys.'
                        }
                        field="fullNodeNewWalletPassphrase"
                        initialValue={fullNodeNewWalletPassphrase}
                        label={'Wallet passphrase'}
                        mb={3}
                        name="fullNodeNewWalletPassphrase"
                        onBlur={this.fullNodeNewWalletPassphraseChanged}
                        isDisabled={fullNodeIsLoading}
                      />
                    </div>
                  ) : null}
                </div>
              ) : !fullNodeIsConnectionSuccess ||
                (fullNodeIsConnectionSuccess &&
                  fullNodeActualAuthKey === fullNodeExpectedAuthKey) ? (
                <Flex justifyContent="center" my={4}>
                  {fullNodeIsLoading ? (
                    <Text textAlign="center">
                      <Spinner />
                      Connecting...
                    </Text>
                  ) : (
                    <Button
                      onClick={this.testConnection}
                      size="medium"
                      type="button"
                      isDisabled={fullNodeIsLoading}
                    >
                      Test
                    </Button>
                  )}
                  <Bar mb={1} mt={4} />
                </Flex>
              ) : null}

              <Bar mb={1} mt={4} />
              <Box mb={1} mt={4}>
                <Text color="gray" mb={1} textAlign="left">
                  fullNodeIsConnectionSuccess: {fullNodeIsConnectionSuccess ? 'Yes' : 'No'}
                  <br />
                  fullNodeActualAuthKey: {fullNodeActualAuthKey}
                  <br />
                  fullNodeExpectedAuthKey: {fullNodeExpectedAuthKey}
                  <br />
                  fullNodeIsCredentialsRequired:{' '}
                  {fullNodeIsCredentialsRequired === null
                    ? 'Unknown'
                    : fullNodeIsCredentialsRequired === true
                    ? 'Yes'
                    : 'No'}
                  <br />
                  fullNodeIsCredentialsSuccess: {fullNodeIsCredentialsSuccess ? 'Yes' : 'No'}
                  <br />
                  fullNodeIsCredentialsSuccess: {fullNodeIsCredentialsSuccess ? 'Yes' : 'No'}
                  <br />
                  fullNodeIsLoading: {fullNodeIsLoading}
                  <br />
                  daemonInfo.walletPath: {daemonInfo.walletPath}
                  <br />
                  daemonInfo.walletFiles:
                  <ul>
                    {daemonInfo.walletFiles.map(w => (
                      <li>{w.namedItem}</li>
                    ))}
                  </ul>
                  <br />
                  fullNodeSelectedWalletName: {fullNodeSelectedWalletName}
                </Text>
              </Box>
            </>
          )
        }}
      </Form>
    )
  }
}
const mapStateToProps = state => ({
  fullNodeConnection: state.fullNodeConnection,
})

export default injectIntl(connect(mapStateToProps)(X1ConnectionDetailsSecureApi))
