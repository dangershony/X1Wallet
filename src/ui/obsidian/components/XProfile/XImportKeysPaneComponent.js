import React from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { Box, Flex } from 'rebass'
import {
  Bar,
  CopyBox,
  DataRow,
  Text,
  Button,
  TextArea,
  Heading,
} from 'components/UI'
import xmessages from './xmessages'
import { closeAllModals } from 'reducers/modal'

const XImportKeysPaneComponent = ({
  intl,
  fullNodeSettings,
  showNotification,
  versionString,
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
  callImportKeys,

  setPastedText,
  // fullNodeKeys
  isImportingKeys,
  pastedText,
  importError,
  importSuccessMessage,
  importedAddresses,

  ...rest
}) => {
  const notifyOfCopy = () =>
    showNotification(intl.formatMessage({ ...xmessages.pubkey_copied_notification_description }))

  const onPassphraseDialogClosed = () => {
    callImportKeys(pastedText)
  }

  const importClick = e => {
    //
    setIsPassphraseDialogOpen(true)
    setCloseAction(onPassphraseDialogClosed)
  }

  const rt = () => {
    return true
  }
  const onFinishClick = () => {
    window.x1Store.dispatch(closeAllModals(rt))
  }

  const onPastedTextChange = e => {
    setPastedText(e.target.value)
  }

  const validate = e => {}
  const mask = value => value && value.trim()

  return (
    <Box as="section" {...rest}>
      <Text fontWeight="normal">
        <FormattedMessage {...xmessages.importkeys_pane_title} />
      </Text>
      <Bar mb={4} mt={2} />

      {importSuccessMessage ? (
        <>
          <Heading.h1 fontSize={20} mt={6} mb={2}>
            Success!
          </Heading.h1>
          <Bar mb={4} mt={2} />
          <DataRow
            left={
              <>
                <Text fontWeight="normal" mb={2}>
                  Target Network
                </Text>
                <Text color="gray" fontWeight="light"></Text>
              </>
            }
            py={2}
            right={fullNodeSettings.network}
          />
          <DataRow
            left={
              <>
                <Text fontWeight="normal" mb={2}>
                  Result
                </Text>
                <Text color="gray" fontWeight="light"></Text>
              </>
            }
            py={2}
            right={importSuccessMessage}
          />
          <Flex mt={4} alignItems="center" as="section" justifyContent="center">
            <Button onClick={onFinishClick}>Finish</Button>
          </Flex>
        </>
      ) : (
        <>
          <DataRow
            left={
              <>
                <Text fontWeight="normal" mb={2}>
                  Target Network
                </Text>
                <Text color="gray" fontWeight="light"></Text>
              </>
            }
            py={2}
            right={fullNodeSettings.network}
          />
          <TextArea
            field={'keys'}
            mt={2}
            mb={4}
            css={`
              word-break: break-all;
            `}
            initialValue={''}
            mask={mask}
            placeholder={'Private Keys'}
            {...rest}
            spellCheck="false"
            validate={validate}
            label={'Private Keys'}
            value={pastedText}
            onChange={onPastedTextChange}
            isDisabled={isImportingKeys}
          />
          <Button
            onClick={importClick}
            isDisabled={isImportingKeys || !pastedText}
            isProcessing={isImportingKeys}
          >
            Import
          </Button>
          {importError ? (
            <>
              <Heading.h1 fontSize={20} mt={6} mb={2}>
                Import failed
              </Heading.h1>
              <Bar mb={4} mt={2} />
              <DataRow
                left={
                  <>
                    <Text fontWeight="normal" mb={2}>
                      Error
                    </Text>
                    <Text color="gray" fontWeight="light"></Text>
                  </>
                }
                py={2}
                right={importError}
              />
            </>
          ) : null}
        </>
      )}
    </Box>
  )
}

XImportKeysPaneComponent.propTypes = {
  fullNodeSettings: PropTypes.object.isRequired,
}

export default injectIntl(XImportKeysPaneComponent)
