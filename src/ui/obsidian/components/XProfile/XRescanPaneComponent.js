import React from 'react'
import PropTypes from 'prop-types'
import { FormattedMessage, injectIntl, intlShape } from 'react-intl'
import { Box, Flex } from 'rebass'
import { Bar, CopyBox, DataRow, Text, Button, TextArea, Heading } from 'components/UI'
import xmessages from './xmessages'
import { closeAllModals } from 'reducers/modal'

const XRescanPaneComponent = ({
  intl,
  fullNodeSettings,
  showNotification,
  versionString,
  setIsPassphraseDialogOpen,
  clearPassphrase,
  setCloseAction,
  callRescan,

  setPastedText,
  // fullNodeKeys
  isRescanRequested,
  rescanError,
  rescanSuccessMessage,

  ...rest
}) => {
  const onPassphraseDialogClosed = () => {
    callRescan()
  }

  const rescanClick = e => {
    setIsPassphraseDialogOpen(true)
    setCloseAction(onPassphraseDialogClosed)
  }

  const rt = () => {
    return true
  }
  const onFinishClick = () => {
    window.x1Store.dispatch(closeAllModals(rt))
  }

  return (
    <Box as="section" {...rest}>
      <Text fontWeight="normal">
        <FormattedMessage {...xmessages.rescan_pane_title} />
      </Text>
      <Bar mb={4} mt={2} />

      {rescanSuccessMessage ? (
        <>
          <Heading.h1 fontSize={20} mt={6} mb={2}>
            Success!
          </Heading.h1>
          <Bar mb={4} mt={2} />
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
            right={rescanSuccessMessage}
          />
          <Flex mt={4} alignItems="center" as="section" justifyContent="center">
            <Button onClick={onFinishClick}>Finish</Button>
          </Flex>
        </>
      ) : (
        <>
          <Button
            onClick={rescanClick}
            isDisabled={isRescanRequested}
            isProcessing={isRescanRequested}
          >
            Rescan
          </Button>
          {rescanError ? (
            <>
              <Heading.h1 fontSize={20} mt={6} mb={2}>
                Rescan could not be started
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
                right={rescanError}
              />
            </>
          ) : null}
        </>
      )}
    </Box>
  )
}

XRescanPaneComponent.propTypes = {
  fullNodeSettings: PropTypes.object.isRequired,
}

export default injectIntl(XRescanPaneComponent)
