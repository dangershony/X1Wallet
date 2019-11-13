import React from 'react'
import PropTypes from 'prop-types'
import { compose } from 'redux'
import { withFormApi, withFormState } from 'informed'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { Box, Flex } from 'rebass'
import { Bar, Button, DataRow, Input, Label, Text, Toggle } from 'components/UI'
import messages from '../renderer/components/Home/messages'
import x1Messages from './x1Xessages'

class X1WalletSettingsFormFullNode extends React.Component {
  static propTypes = {
    autopilotDefaults: PropTypes.object.isRequired,
    formApi: PropTypes.object.isRequired,
    formState: PropTypes.object.isRequired,
    intl: intlShape.isRequired,
    wallet: PropTypes.object.isRequired,
  }

  render() {
    const { intl, formState, wallet } = this.props
    const { chain, network } = wallet

    return (
      <>
        <Box as="section" mb={4}>
          <Text fontWeight="normal">
            <FormattedMessage {...messages.section_basic_title} />
          </Text>
          <Bar mb={4} mt={2} />
          <DataRow
            left={<FormattedMessage {...x1Messages.X1_walletsettingsformfullnode_wallettype} />}
            py={2}
            right={'Full Node'}
          />
          <DataRow left={<FormattedMessage {...messages.chain} />} py={2} right={chain} />
          <DataRow left={<FormattedMessage {...messages.network} />} py={2} right={network} />
          <DataRow
            left={<FormattedMessage {...messages.host} />}
            py={2}
            right={wallet.fullNodeHost}
          />
          <Input field="fullNodeHost" type="hidden" id="fullNodeHost" />

          <DataRow
            left={
              <Label htmlFor="fullNodeWalletFilePath" mb={2}>
                Auth Key
              </Label>
            }
            py={2}
            right={wallet.fullNodeExpectedAuthKey}
          />

          <Input field="fullNodeExpectedAuthKey" type="hidden" id="fullNodeExpectedAuthKey" />
        </Box>

        <Box as="section" mb={4}>
          <Text fontWeight="normal">
            <FormattedMessage {...messages.section_naming_title} />
          </Text>
          <Bar mb={4} mt={2} />

          <DataRow
            left={
              <>
                <Label htmlFor="name" mb={2}>
                  <FormattedMessage {...messages.wallet_settings_name_label} />
                </Label>
                <Text color="gray" fontWeight="light">
                  The wallet name as defined on the Full Node.
                </Text>
              </>
            }
            py={2}
            right={
              <Input
                allowEmptyString
                field="name"
                id="name"
                justifyContent="flex-end"
                maxLength={30}
                ml="auto"
                placeholder={intl.formatMessage({
                  ...messages.wallet_settings_name_placeholder,
                })}
                textAlign="right"
                width={250}
              />
            }
          />
          <DataRow
            left={
              <Label htmlFor="fullNodeWalletFilePath" mb={2}>
                Full Node Path
              </Label>
            }
            py={2}
            right={wallet.fullNodeWalletFilePath}
          />

          <Input field="fullNodeWalletFilePath" type="hidden" id="fullNodeWalletFilePath" />

          <DataRow
            left={
              <>
                <Label htmlFor="fullNodeUser" mb={2}>
                  API User
                </Label>
                <Text color="gray" fontWeight="light">
                  The configured SecureApi user name.
                </Text>
              </>
            }
            py={2}
            right={
              <Input
                allowEmptyString
                field="fullNodeUser"
                id="fullNodeUser"
                justifyContent="flex-end"
                maxLength={30}
                ml="auto"
                placeholder="User name"
                textAlign="right"
                width={250}
              />
            }
          />

          <DataRow
            left={
              <>
                <Label htmlFor="fullNodePwd" mb={2}>
                  API Password
                </Label>
                <Text color="gray" fontWeight="light">
                  The configured SecureApi password.
                </Text>
              </>
            }
            py={2}
            right={
              <Input
                type="password"
                allowEmptyString
                field="fullNodePwd"
                id="fullNodePwd"
                justifyContent="flex-end"
                maxLength={30}
                ml="auto"
                placeholder="Password"
                textAlign="right"
                width={250}
              />
            }
          />

          {true ? (
            <p></p>
          ) : (
            <DataRow
              left={
                <>
                  <Label htmlFor="alias" mb={2}>
                    JSON
                  </Label>
                  <Text color="gray" fontWeight="light"></Text>
                </>
              }
              py={2}
              right={
                <Input
                  allowEmptyString
                  field="alias"
                  id="alias"
                  justifyContent="flex-end"
                  ml="auto"
                  placeholder={intl.formatMessage({
                    ...messages.wallet_settings_alias_placeholder,
                  })}
                  textAlign="right"
                  width={250}
                />
              }
            />
          )}
        </Box>
      </>
    )
  }
}

export default compose(
  withFormApi,
  withFormState,
  injectIntl
)(X1WalletSettingsFormFullNode)
