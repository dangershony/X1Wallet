import React from 'react'
import PropTypes from 'prop-types'
import { connect } from 'react-redux'
import { animated, Transition } from 'react-spring/renderprops.cjs'
import styled from 'styled-components'
import { closeModal, modalSelectors } from 'reducers/modal'
import { Modal, ModalOverlayStyles } from 'components/UI'
import { useOnKeydown } from 'hooks'
import Autopay from 'containers/Autopay'
import Pay from 'containers/Pay'
import Request from 'containers/Request'
import Channels from 'containers/Channels'
import ChannelDetailModal from 'containers/Channels/ChannelDetailModal'
import ChannelCreate from 'containers/Channels/ChannelCreate'
import ReceiveModal from 'containers/Wallet/ReceiveModal'
import ActivityModal from 'containers/Activity/ActivityModal'
import SettingsPage from 'containers/Settings/SettingsPage'
import ProfilePage from 'containers/Profile/ProfilePage'

// Obsidian modals
import XActivityModal from '../../obsidian/components/XActivity/XActivityModal/XActivityModalContainer'
import XWalletReceiveModalContainer from '../../obsidian/components/XWallet/XWalletReceiveModalContainer'
import XPayContainer from '../../obsidian/components/XPay/XPayContainer'
import XProfilePageContainer from '../../obsidian/components/XProfile/XProfilePageContainer'

const Container = styled(animated.div)`
  ${ModalOverlayStyles}
`

const ModalContent = ({ type, options, closeModal }) => {
  switch (type) {
    case 'SETTINGS':
      return (
        <Modal onClose={closeModal}>
          <SettingsPage />
        </Modal>
      )

    case 'PROFILE':
      return (
        <Modal onClose={closeModal}>
          <ProfilePage />
        </Modal>
      )

    case 'AUTOPAY':
      return (
        <Modal onClose={closeModal} pt={4}>
          <Autopay width={1} />
        </Modal>
      )

    case 'PAY_FORM':
      return (
        <Modal onClose={closeModal} p={4}>
          <Pay mx="auto" width={9 / 16} />
        </Modal>
      )

    case 'REQUEST_FORM':
      return (
        <Modal onClose={closeModal} p={4}>
          <Request mx="auto" width={9 / 16} />
        </Modal>
      )

    case 'RECEIVE_MODAL':
      return (
        <Modal onClose={closeModal} p={4}>
          <ReceiveModal mx="auto" width={9 / 16} />
        </Modal>
      )

    case 'ACTIVITY_MODAL':
      return (
        <Modal onClose={closeModal} p={4}>
          <ActivityModal mx="auto" width={9 / 16} />
        </Modal>
      )

    case 'CHANNELS':
      return (
        <Modal onClose={closeModal}>
          <Channels width={1} />
        </Modal>
      )

    case 'CHANNEL_CREATE':
      return (
        <Modal onClose={closeModal} py={4}>
          <ChannelCreate onSubmit={closeModal} width={1} />
        </Modal>
      )

    case 'CHANNEL_DETAIL':
      return (
        <Modal onClose={closeModal} p={4}>
          <ChannelDetailModal type="CHANNEL_DETAIL" width={1} />
        </Modal>
      )
    // Obsidian modals
    case 'XACTIVITY_MODAL':
      return (
        <Modal onClose={closeModal} p={4}>
          <XActivityModal mx="auto" width={12 / 16} />
        </Modal>
      )

    case 'XRECEIVE_MODAL':
      return (
        <Modal onClose={closeModal} p={4}>
          <XWalletReceiveModalContainer mx="auto" width={12 / 16} />
        </Modal>
      )
    case 'XPAY_MODAL':
      return (
        <Modal onClose={closeModal} p={4}>
          <XPayContainer mx="auto" width={12 / 16} />
        </Modal>
      )
    case 'XPROFILE_MODAL':
      return (
        <Modal onClose={closeModal}>
          <XProfilePageContainer options={options} />
        </Modal>
      )
  }
}

ModalContent.propTypes = {
  closeModal: PropTypes.func.isRequired,
  type: PropTypes.string.isRequired,
}

/**
 * ModalStack - Render modqals from the modal stack.
 *
 * @param {{ modals, closeModal }} props Props
 * @returns {Node} Node
 */
function ModalStack(props) {
  const { modals, closeModal } = props
  const doCloseModal = () => closeModal()

  useOnKeydown('Escape', closeModal)
  return (
    <Transition
      enter={{ opacity: 1, pointerEvents: 'auto' }}
      from={{ opacity: 0, pointerEvents: 'auto' }}
      items={modals}
      keys={item => item.id}
      leave={{ opacity: 0, pointerEvents: 'none' }}
    >
      {modal =>
        modal &&
        /* eslint-disable react/display-name */
        (styles => (
          <Container style={styles}>
            <ModalContent closeModal={doCloseModal} type={modal.type} options={modal.options} />
          </Container>
        ))
      }
    </Transition>
  )
}

ModalStack.propTypes = {
  closeModal: PropTypes.func.isRequired,
  modals: PropTypes.array.isRequired,
}

const mapStateToProps = state => ({
  modals: modalSelectors.getModalState(state),
})

const mapDispatchToProps = {
  closeModal,
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(ModalStack)
