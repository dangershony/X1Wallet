import React from 'react'
import PropTypes from 'prop-types'
import { Box } from 'rebass'
import ConnectionDetailsContext from '../renderer/components/Onboarding/Steps/ConnectionDetailsContext'
import X1ConnectionDetailsSecureApi from './X1ConnectionDetailsSecureApi'

class X1ConnectionDetails extends React.Component {
  state = {
    formType: null,
  }

  static propTypes = {
    wizardApi: PropTypes.object,
    wizardState: PropTypes.object,
  }

  componentDidMount() {
    this.openModal('FORM_TYPE_OBSIDIAN')
  }

  componentDidUpdate(prevProps, prevState) {
    const {} = this.props
    const { formType } = this.state
    if (formType && formType !== prevState.formType && prevState.formType) {
      switch (formType) {
        case 'FORM_TYPE_OBSIDIAN':
          break
      }
    }
  }

  openModal = formType => {
    this.setState({ formType })
  }

  render() {
    const { wizardApi, wizardState } = this.props
    const { formType } = this.state

    if (!formType) {
      return null
    }

    return (
      <Box
        css={`
          visibility: ${false ? 'hidden' : 'visible'};
        `}
        width={1}
      >
        <ConnectionDetailsContext.Provider
          value={{
            formType,
            openModal: this.openModal,
          }}
        >
          {formType === 'FORM_TYPE_OBSIDIAN' ? (
            <X1ConnectionDetailsSecureApi wizardApi={wizardApi} wizardState={wizardState} />
          ) : (
            <React.Fragment />
          )}
        </ConnectionDetailsContext.Provider>
      </Box>
    )
  }
}

export default X1ConnectionDetails
