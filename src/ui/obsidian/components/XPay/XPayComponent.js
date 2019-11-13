import React from 'react'
import debounce from 'lodash/debounce'
import { Box, Flex } from 'rebass'
import { animated, Keyframes, Transition } from 'react-spring/renderprops.cjs'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { convert } from '@zap/utils/btc'
import { Bar, Form, Message, Input, Panel, Text, Button } from 'components/UI'
import { CurrencyFieldGroup, CryptoValue, FiatValue } from 'containers/UI'
import XPayButtonsComponent from './XPayButtonsComponent'
import XPayHeaderComponent from './XPayHeaderComponent'
import xmessages from './xmessages'
import BigArrowRight from 'components/Icon/BigArrowRight'
import { DataRow, Heading } from 'components/UI'

/**
 * Animation to handle showing/hiding the payReq field.
 */
const ShowHidePayReq = Keyframes.Spring({
  small: { height: 48 },
  big: async (next, cancel, ownProps) => {
    ownProps.context.focusPayReqInput()
    await next({ height: 110, immediate: true })
  },
})

/**
 * Animation to handle showing/hiding the form buttons.
 */
const ShowHideButtons = Keyframes.Spring({
  show: { opacity: 1 },
  hide: { opacity: 0 },
})

/**
 * Animation to handle showing/hiding the amount fields.
 */
const ShowHideAmount = Keyframes.Spring({
  show: async (next, cancel, ownProps) => {
    await next({ display: 'block' })
    ownProps.context.focusAmountInput()
    await next({ opacity: 1, height: 'auto' })
  },
  hide: { opacity: 0, height: 0, display: 'none' },
  remove: { opacity: 0, height: 0, display: 'none', immediate: true },
})

class XPayComponent extends React.Component {
  
  state = {
    currentStep: 'address',
    previousStep: null,
  }

  amountInput = React.createRef()
  payReqInput = React.createRef()

  updateFees = debounce(() => {
    const amountInSats = this.amountInSats()
    const formState = this.formApi.getState()
    const { payReq: address } = formState.values
    this.props.callBuildTransaction({
      sign: false,
      send: false,
      recipients: [{ address: address, amount: amountInSats }],
    })
  }, 500)

  onPassphraseDialogClosed = () => {
    const { closeModal, clearPassphrase, passphrase } = this.props
    const amountInSats = this.amountInSats()
    const formState = this.formApi.getState()
    const { payReq: address } = formState.values
    this.props.callBuildTransaction({
      sign: true,
      send: true,
      passphrase: passphrase,
      recipients: [{ address: address, amount: amountInSats }],
    })
    // changeFilter('ALL_ACTIVITY') todo
    clearPassphrase()
    closeModal()
  }

  // Set a flag so that we can trigger form submission in componentDidUpdate once the form is loaded.
  componentDidMount() {
    const { fetchTickers } = this.props
    fetchTickers()
  }

  componentDidUpdate(prevProps, prevState) {
    const { transactionResponse, cryptoUnitName } = this.props

    const { currentStep, address, amount } = this.state

    // If payReq address or amount has has changed update the relevant form values.
    const isChangedAddress = prevState.address !== address
    const isChangedAmount = prevState.amount !== amount
    if (isChangedAddress || isChangedAmount) {
      return this.autoFillForm(address, amount)
    }

    // If we have gone back to the address step, unmark all fields from being touched.
    if (currentStep !== prevState.currentStep) {
      if (currentStep === 'address') {
        Object.keys(this.formApi.getState().touched).forEach(field => {
          this.formApi.setTouched(field, false)
        })
      }
    }

    // If we now have a valid onchain address, trigger the form submit to move to the amount step.
    if (currentStep === 'address') {
      this.formApi.submitForm()
    }

    const amountToSend = this.amountInSats()
    const totalFee = transactionResponse.fee
    const totalCost = amountToSend + totalFee
    const fullNodeCryptoUnitName = window.x1Tools.translateCryptoUnitName(cryptoUnitName)

    if (amountToSend !== this.state.amountToSend) {
      this.setState({
        amountToSend: amountToSend,
        totalFee: totalFee,
        totalCost: totalCost,
        serializedSize: transactionResponse && transactionResponse.serializedSize,
        virtualSize: transactionResponse && transactionResponse.virtualSize,
        transactionId: transactionResponse && transactionResponse.transactionId,
        fullNodeCryptoUnitName: fullNodeCryptoUnitName,
      })
    }
  }

  autoFillForm = (address, amount) => {
    if (address && amount) {
      this.setState({ currentStep: 'address' }, () => {
        this.formApi.reset()
        this.formApi.setValue('payReq', address)
        this.formApi.setValue('amountCrypto', amount)
        this.formApi.submitForm()
      })
    } else if (address) {
      this.setState({ currentStep: 'address' }, () => {
        this.formApi.reset()
        this.formApi.setValue('payReq', address)
      })
    }
  }

  amountInSats = () => {
    const { cryptoUnit } = this.props
    const amount = this.formApi.getValue('amountCrypto')
    return convert(cryptoUnit, 'sats', amount)
  }

  

  /**
   * onSubmit - Form submit handler.
   *
   * @param  {object} values Submitted form values.
   */
  onSubmit = values => {
    const { currentStep } = this.state
    const { setIsPassphraseDialogOpen, setCloseAction } = this.props
    if (currentStep === 'summary') {
      setIsPassphraseDialogOpen(true)
      setCloseAction(this.onPassphraseDialogClosed)
    } else {
      this.nextStep()
    }
  }

  /**
   * setFormApi - Store the formApi on the component context to make it available at this.formApi.
   */
  setFormApi = formApi => {
    this.formApi = formApi
  }

  focusPayReqInput = () => {
    if (this.payReqInput.current) {
      this.payReqInput.current.focus()
    }
  }

  focusAmountInput = () => {
    if (this.amountInput.current) {
      this.amountInput.current.focus()
    }
  }

  steps = () => {
    return ['address', 'amount', 'summary']
  }

  previousStep = () => {
    const { currentStep } = this.state
    const nextStep = Math.max(this.steps().indexOf(currentStep) - 1, 0)
    if (currentStep !== nextStep) {
      this.setState({ currentStep: this.steps()[nextStep], previousStep: currentStep })
    }
  }

  nextStep = () => {
    const { currentStep } = this.state
    const nextStepIndex = Math.min(this.steps().indexOf(currentStep) + 1, this.steps().length - 1)
    const nextStep = this.steps()[nextStepIndex]
    if (currentStep !== nextStepIndex) {
      this.setState({ currentStep: nextStep, previousStep: currentStep })
    }
  }

  handleAddressChanged = payReq => {
    const state = {
      currentStep: 'address',
      address: payReq,
    }
    this.setState(state)
  }

  renderHelpText = () => {
    const { chainName, cryptoUnitName } = this.props
    const { currentStep, previousStep } = this.state

    return (
      <Transition
        enter={{ opacity: 1, height: 80 }}
        from={{ opacity: 0, height: 0 }}
        initial={{ opacity: 1, height: 80 }}
        items={currentStep === 'address'}
        leave={{ opacity: 0, height: 0 }}
        native
      >
        {show =>
          show &&
          (styles => (
            <animated.div style={styles}>
              <Text mb={4}>
                <FormattedMessage
                  {...xmessages.description}
                  values={{ chain: chainName, ticker: cryptoUnitName }}
                />
              </Text>
            </animated.div>
          ))
        }
      </Transition>
    )
  }

  renderAddressField = () => {
    const { currentStep } = this.state
    const { chain, redirectPayReq, network, intl } = this.props

    return (
      <Box className={currentStep === 'summary' ? 'element-hide' : 'element-show'}>
        <ShowHidePayReq context={this} state={currentStep === 'address' ? 'big' : 'small'}>
          {styles => (
            <React.Fragment>
              <Input
                chain={chain}
                css={`
                  resize: vertical;
                  min-height: 48px;
                `}
                field="payReq"
                forwardedRef={this.payReqInput}
                initialValue={''}
                isReadOnly={currentStep !== 'address'}
                isRequired
                label={intl.formatMessage({ ...xmessages['request_label_onchain'] })}
                name="payReq"
                network={network}
                onValueChange={this.handleAddressChanged}
                style={styles}
                validateOnBlur
                validateOnChange
                width={1}
                willAutoFocus
              />
            </React.Fragment>
          )}
        </ShowHidePayReq>
      </Box>
    )
  }

  renderAmountFields = (cryptoUnit, cryptoUnitName) => {
    const { currentStep } = this.state
    let { intl, initialAmountCrypto, initialAmountFiat, transactionResponse } = this.props

    const formState = this.formApi.getState()

    const amountToSend = this.amountInSats()
    const totalFee = transactionResponse.fee
    const totalCost = amountToSend + totalFee
    const fullNodeCryptoUnitName = window.x1Tools.translateCryptoUnitName(cryptoUnitName)

    return (
      <ShowHideAmount
        context={this}
        state={currentStep === 'amount' ? 'show' : currentStep === 'address' ? 'hide' : 'remove'}
      >
        {styles => (
          <Box style={styles}>
            <Bar my={3} variant="light" />

            <CurrencyFieldGroup
              formApi={this.formApi}
              forwardedRef={this.amountInput}
              initialAmountCrypto={initialAmountCrypto}
              initialAmountFiat={initialAmountFiat}
              isDisabled={currentStep !== 'amount'}
              isRequired
              onChange={this.updateFees}
            />

            <DataRow
              mt={3}
              left={<FormattedMessage {...xmessages.fee} />}
              right={
                <React.Fragment>
                  <Box style={{ opacity: 1 }}>
                    <Text color={'#fff'} mb={1} textAlign="right">
                      {`+ `}
                      <CryptoValue value={totalFee} />
                      <i> {fullNodeCryptoUnitName}</i>
                    </Text>
                    <Text color="white" fontSize="xs" fontWeight="normal" textAlign="right">
                      <FiatValue style="currency" value={totalFee} />
                    </Text>
                  </Box>
                </React.Fragment>
              }
            />
            <DataRow
              left={<FormattedMessage {...xmessages.total} />}
              right={
                <React.Fragment>
                  <Box style={{ opacity: 1 }}>
                    <Text color={'#fff'} mb={1} textAlign="right">
                      {`= `}
                      <CryptoValue value={totalCost} />
                      <i> {fullNodeCryptoUnitName}</i>
                    </Text>
                    <Text color="white" fontSize="xs" fontWeight="normal" textAlign="right">
                      <FiatValue style="currency" value={totalCost} cryptoUnit={cryptoUnit} />
                    </Text>
                  </Box>
                </React.Fragment>
              }
            />
            <Bar my={3} variant="light" />
          </Box>
        )}
      </ShowHideAmount>
    )
  }

  renderSummary = () => {
    const { currentStep } = this.state
    let { routes, lndTargetConfirmations, isQueryingFees, onchainFees } = this.props

    const formState = this.formApi.getState()
    let { speed, payReq } = formState.values
    speed = 'TRANSACTION_SPEED_SLOW'
    const address = payReq

    const render = () => {
      // convert entered amount to satoshis
      const amount = this.amountInSats()

      return (
        <Box>
          <Box py={3}>
            <Flex alignItems="center" flexDirection="column">
              <Flex alignItems="center">
                <Box width={1} alignItems="center" textAlign="center" mb={4}>
                  <CryptoValue fontSize="xxl" value={amount} />
                  <Text color="gray">
                    {' ≈ '}
                    <FiatValue style="currency" value={amount} />
                  </Text>
                </Box>
              </Flex>

              <Box width={1} mb={4}>
                <Text color="lightningOrange" textAlign="center">
                  <BigArrowRight height="28px" width="40px" />
                </Text>
              </Box>
              <Box width={1} mb={4}>
                <Text className="hint--bottom-left" textAlign="center" fontSize="xl">
                  {address}
                  {/* <Truncate text={address} /> */}
                </Text>
              </Box>
            </Flex>
          </Box>
          <Bar variant="light" />
          <DataRow
            mt={3}
            left={<FormattedMessage {...xmessages.fee} />}
            right={
              <React.Fragment>
                <Box style={{ opacity: 1 }}>
                  <Text color={'grey'} mb={1} textAlign="right">
                    {`+ `}
                    <CryptoValue value={this.state.totalFee} />
                    <i> {this.state.fullNodeCryptoUnitName}</i>
                  </Text>
                  <Text color="grey" fontSize="xs" fontWeight="normal" textAlign="right">
                    <FiatValue
                      style="currency"
                      value={this.state.totalFee}
                      cryptoUnit={this.state.fullNodeCryptoUnitName}
                    />
                  </Text>
                </Box>
              </React.Fragment>
            }
          />
          <DataRow
            left={<FormattedMessage {...xmessages.total} />}
            right={
              <React.Fragment>
                <Box style={{ opacity: 1 }}>
                  <Text color={'grey'} mb={1} textAlign="right">
                    {`= `}
                    <CryptoValue value={this.state.totalCost} />
                    <i> {this.state.fullNodeCryptoUnitName}</i>
                  </Text>
                  <Text color="grey" fontSize="xs" fontWeight="normal" textAlign="right">
                    <FiatValue
                      style="currency"
                      value={this.state.totalCost}
                      cryptoUnit={this.state.fullNodeCryptoUnitName}
                    />
                  </Text>
                </Box>
              </React.Fragment>
            }
          />
          <DataRow
            left={<FormattedMessage {...xmessages.virtualSize} />}
            right={
              <React.Fragment>
                <Box style={{ opacity: 1 }}>
                  <Text color="grey" fontSize="xm" fontWeight="normal" textAlign="right">
                    {this.state.virtualSize}
                  </Text>
                </Box>
              </React.Fragment>
            }
          />
          <DataRow
            left={<FormattedMessage {...xmessages.serializeSize} />}
            right={
              <React.Fragment>
                <Box style={{ opacity: 1 }}>
                  <Text color="grey" fontSize="m" fontWeight="normal" textAlign="right">
                    {this.state.serializedSize}
                  </Text>
                </Box>
              </React.Fragment>
            }
          />
          <Bar my={3} variant="light" />
        </Box>
      )
    }

    return (
      <Transition
        enter={{ opacity: 1, height: 'auto' }}
        from={{ opacity: 0, height: 0 }}
        initial={{ opacity: 1, height: 'auto' }}
        items={currentStep === 'summary'}
        leave={{ opacity: 0, height: 0 }}
        native
      >
        {show => show && (styles => <animated.div style={styles}>{render()}</animated.div>)}
      </Transition>
    )
  }

  render() {
    const { currentStep } = this.state
    let {
      closeModal,
      cryptoUnit,
      cryptoUnitName,
      fetchTickers,
      chainName,

      walletInfo,
      transactionResponse,
      fullNodeIsLoading,

      callBuildTransaction,
      setCloseAction,
      clearPassphrase,
      setIsPassphraseDialogOpen,

      ...rest
    } = this.props

    const { stakable, confirmed, pending, spendable } = walletInfo.walletDetails.balance
    const total = confirmed + pending
    return (
      <Form
        css={`
          height: 100%;
        `}
        width={1}
        {...rest}
        getApi={this.setFormApi}
        onSubmit={this.onSubmit}
      >
        {({ formState }) => {
          // Deterine which buttons should be visible.
          const hasBackButton = currentStep !== 'address'
          const hasSubmitButton = currentStep !== 'address'

          // convert entered amount to satoshis
          let amountInSats = this.amountInSats()

          // Determine whether we have enough funds available.
          let hasEnoughFunds = this.state.totalCost <= spendable

          // Determine what the text should be for the next button.
          let nextButtonText = <FormattedMessage {...xmessages.next} />
          if (currentStep === 'summary') {
            nextButtonText = (
              <>
                <FormattedMessage {...xmessages.send} />
                {` `}
                <CryptoValue value={amountInSats} />
                {` `}
                {cryptoUnitName}
              </>
            )
          }
          return (
            <Panel>
              <Panel.Header>
                <XPayHeaderComponent
                  title={
                    <>
                      <FormattedMessage {...xmessages.send} /> {chainName} ({cryptoUnitName})
                    </>
                  }
                  type={'onchain'}
                />
                <Bar mt={2} />
              </Panel.Header>

              <Panel.Body py={3}>
                {this.renderHelpText()}
                {this.renderAddressField()}
                {this.renderAmountFields(cryptoUnit, cryptoUnitName)}
                {this.renderSummary()}

                <React.Fragment>
                  <Heading.h4 fontWeight="normal" mt={3} textAlign="center">
                    <FormattedMessage {...xmessages.current_balance} />:
                  </Heading.h4>

                  <Text fontSize="m" textAlign="center">
                    <CryptoValue value={spendable} />
                    {` `}
                    {cryptoUnitName} (spendale),
                  </Text>
                  <Text fontSize="s" textAlign="center">
                    <CryptoValue value={confirmed} />
                    {` `}
                    {cryptoUnitName} (confirmed)
                  </Text>
                  <Text fontSize="s" textAlign="center">
                    <CryptoValue value={pending} />
                    {` `}
                    {cryptoUnitName} (pending)
                  </Text>
                </React.Fragment>
              </Panel.Body>
              <Panel.Footer>
                <ShowHideButtons state={hasBackButton || hasSubmitButton ? 'show' : 'show'}>
                  {styles => (
                    <Box style={styles}>
                      {currentStep === 'summary' && !hasEnoughFunds && (
                        <Message justifyContent="center" mb={2} variant="error">
                          <FormattedMessage {...xmessages.error_not_enough_funds} />
                        </Message>
                      )}

                      <XPayButtonsComponent
                        hasBackButton={hasBackButton}
                        hasSubmitButton={hasSubmitButton}
                        isDisabled={
                          formState.pristine ||
                          formState.invalid ||
                          fullNodeIsLoading ||
                          (currentStep === 'summary' && !hasEnoughFunds)
                        }
                        isProcessing={fullNodeIsLoading}
                        nextButtonText={nextButtonText}
                        previousStep={this.previousStep}
                      />
                    </Box>
                  )}
                </ShowHideButtons>
              </Panel.Footer>
            </Panel>
          )
        }}
      </Form>
    )
  }
}

export default injectIntl(XPayComponent)
