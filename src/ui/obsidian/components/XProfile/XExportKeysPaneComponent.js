import React from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { Box, Card, Flex } from 'rebass'
import { Bar, CopyBox, DataRow, Text, Button, TextArea, Heading, CopyButton } from 'components/UI'
import xmessages from './xmessages'
import { closeAllModals } from 'reducers/modal'

const XExportKeysPaneComponent = ({
  intl,
  fullNodeSettings,
  showNotification,
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
  callExportKeys,

  // fullNodeKeys
  isExportingKeys,
  exportError,
  exportSuccessMessage,
  exportedKeys,

  ...rest
}) => {
  const notifyOfCopy = () =>
    showNotification(intl.formatMessage({ ...xmessages.export_keys_copy_notification }))

  const onPassphraseDialogClosed = () => {
    callExportKeys()
  }

  const exportClick = e => {
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

  const onCopy = () => {
    notifyOfCopy()
  }
  const validate = e => {}
  const mask = value => value && value.trim()

  return (
    <Box as="section" {...rest}>
      <Text fontWeight="normal">
        <FormattedMessage {...xmessages.exportkeys_pane_title} />
      </Text>
      <Bar mb={4} mt={2} />

      {exportSuccessMessage ? (
        <>
          <Heading.h1 fontSize={20} mt={4} mb={2}>
            Success!
          </Heading.h1>
         

          <Flex justifyContent="space-between">
            <TextArea
              css={`
                word-break: break-all;
              `}
              spellCheck="false"
              fontSize="m"
              textAlign="left"
              width={1}
              value={exportSuccessMessage}
              rows={20}
            ></TextArea>
            <Box bg="primaryColor">
              <CopyButton hint={'Copy'} onCopy={onCopy} p={3} value={exportSuccessMessage} />
            </Box>
          </Flex>

          <Flex mt={4} alignItems="center" as="section" justifyContent="center">
            <Button onClick={onFinishClick}>Finish</Button>
          </Flex>
        </>
      ) : (
        <>
          
          <Button onClick={exportClick} isDisabled={isExportingKeys} isProcessing={isExportingKeys}>
            Export
          </Button>
          {exportError ? (
            <>
              <Heading.h1 fontSize={20} mt={6} mb={2}>
                Export failed
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
                right={exportError}
              />
            </>
          ) : null}
        </>
      )}
    </Box>
  )
}

XExportKeysPaneComponent.propTypes = {
  fullNodeSettings: PropTypes.object.isRequired,
}

export default injectIntl(XExportKeysPaneComponent)
