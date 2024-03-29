import React from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage } from 'react-intl'
import { Bar, Form, Header, Message, RadioGroup, Radio } from 'components/UI'
import { isMainnetAsDefault } from '@zap/utils/featureFlag'
import messages from './messages'

class Network extends React.Component {
  static propTypes = {
    network: PropTypes.string,
    setNetwork: PropTypes.func.isRequired,
    wizardApi: PropTypes.object,
    wizardState: PropTypes.object,
  }

  static defaultProps = {
    wizardApi: {},
    wizardState: {},
  }

  handleSubmit = values => {
    const { setNetwork } = this.props
    const { network } = values
    setNetwork(network)
  }

  setFormApi = formApi => {
    this.formApi = formApi
  }

  render() {
    const { wizardApi, wizardState, network, setNetwork, ...rest } = this.props
    const { getApi, onChange, onSubmit, onSubmitFailure } = wizardApi
    const { currentItem } = wizardState
    const networkTypes = [
      <Radio
        key="mainnet"
        description=<FormattedMessage {...messages.network_mainnet_description} />
        label={<FormattedMessage {...messages.network_mainnet} />}
        value="mainnet"
      />,
      <Radio
        key="testnet"
        description=<FormattedMessage {...messages.network_testnet_description} />
        label={<FormattedMessage {...messages.network_testnet} />}
        value="testnet"
      />,
    ]
    return (
      <Form
        {...rest}
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
        width={1}
      >
        {({ formState }) => {
          const isMainnet = formState.values.network === 'mainnet'
          return (
            <>
              <Header
                subtitle={<FormattedMessage {...messages.network_description} />}
                title={<FormattedMessage {...messages.network_title} />}
              />
              <Bar my={4} />
              <RadioGroup field="network" initialValue={network} isRequired name="network">
                {isMainnetAsDefault() ? networkTypes : networkTypes.reverse()}
              </RadioGroup>

              {isMainnet && (
                <Message justifyContent="center" mt={3} variant="warning">
                  <FormattedMessage {...messages.network_warning} />
                </Message>
              )}
            </>
          )
        }}
      </Form>
    )
  }
}

export default Network
