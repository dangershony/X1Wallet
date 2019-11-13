import React from 'react'
import { useRef } from 'react'
import PropTypes from 'prop-types'
import { Flex, Box } from 'rebass'
import { FormattedMessage, injectIntl } from 'react-intl'
import { withRouter } from 'react-router-dom'
import Delete from 'components/Icon/Delete'
import CheckCircle from 'components/Icon/CheckCircle'
import { Dialog, Heading, DialogOverlay, Text, Input, Form, Button } from 'components/UI'
import xmessages from './xmessages'
import messages from '../../../renderer/components/Home/messages'
import { vcl } from '../../crypto/visualcrypt-light'

class XPassphrase extends React.Component {
  constructor(props) {
    super(props)
    this.state = {
      isCorrect: false,
      hasTried: false,
    }
  }

  setFormApi = formApi => {
    this.formApi = formApi
  }

  handleOk = formFiels => {
    if (!this.state.isCorrect) {
      this.setState({ hasTried: true })
      this.formApi.setError('password', 'The passphrase is not correct.')
      return
    }
    this.props.setIsPassphraseDialogOpen(false)
    this.props.closeAction()
    this.setState({ isCorrect: false, hasTried: false })
  }

  handleCancel = () => {
    this.props.setIsPassphraseDialogOpen(false)
  }

  onChange = e => {
    const { value } = e.target
    this.props.setPassphrase(value)

    if (!value || !value.trim()) {
      this.formApi.setError('password', undefined)
      return
    }

    if (this.check(value)) {
      this.setState({ isCorrect: true })
      this.formApi.setError('password', undefined)
    } else {
      this.setState({ isCorrect: false })
      if (this.state.hasTried) {
        this.formApi.setError('password', 'The passphrase is not correct.')
      }
    }
  }

  check = passphrase => {
    // todo: make normalizing match exactly the spec!
    const visualCryptNormalizedPasswordString = passphrase.trim()
    const keyMaterial64 = vcl.hashPassword(visualCryptNormalizedPasswordString)
    try {
      vcl.decryptWithRawKey(this.props.passphraseChallenge, keyMaterial64)
      return true
    } catch (e) {
      // We want to show all errors as notification, except the standard message.
      if (e.message !== 'Either the key or the data is not valid.') {
        this.props.showError(e.message)
      }
      return false
    }
  }

  render() {
    const { intl, showError, isDialogOpen, ...rest } = this.props
    if (!isDialogOpen) {
      return null
    }

    const header = (
      <Flex alignItems="center" flexDirection="column" mb={4}>
        <Box color="lightningOrange" mb={2}>
          {this.state.isCorrect ? (
            <CheckCircle height={72} width={72} />
          ) : (
            <Delete height={72} width={72} />
          )}
        </Box>
        <Heading.h1>
          <FormattedMessage {...xmessages.lnd_crashed_dialog_header} />
        </Heading.h1>
      </Flex>
    )

    const body = (
      <Form
        key={`wallet-unlocker-form-walletID`}
        getApi={this.setFormApi}
        onSubmit={this.handleOk}
        {...rest}
      >
        {({ formState }) => (
          <>
            {/*  <WalletHeader wallet={wallet} /> */}
            <Input
              field="password"
              id="password"
              isRequired
              label={<FormattedMessage {...messages.wallet_unlocker_password_label} />}
              minLength={0}
              my={3}
              placeholder={intl.formatMessage({ ...messages.wallet_unlocker_password_placeholder })}
              type="password"
              validateOnBlur
              validateOnChange={formState.invalid}
              willAutoFocus={true}
              willAutoFocusImportant={true}
              onChange={this.onChange}
            />

            <Button isDisabled={!this.props.passphrase} isProcessing={false} type="submit">
              <FormattedMessage {...messages.wallet_unlocker_button_label} />
            </Button>
          </>
        )}
      </Form>
    )

    return (
      <DialogOverlay alignItems="center" justifyContent="center">
        <Dialog
          buttons={
            [] /*[
            {
              name: <FormattedMessage {...xmessages.lnd_crashed_dialog_button_text} />,
              onClick: this.handleClose,
            },
          ]*/
          }
          header={header}
          onClose={this.handleCancel}
          width={640}
        >
          {body}
        </Dialog>
      </DialogOverlay>
    )
  }
}

export default withRouter(injectIntl(XPassphrase))
