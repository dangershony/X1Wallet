import React from 'react'
import PropTypes from 'prop-types'
import { Box } from 'rebass'
import XTransactionModal from '../XTransaction/XTransactionModal'

export class XActivityModal extends React.PureComponent {
  static propTypes = {
    item: PropTypes.object,
    networkInfo: PropTypes.shape({
      id: PropTypes.string,
      name: PropTypes.string,
    }),
    showNotification: PropTypes.func.isRequired,
  }

  render() {
    
    const { item, networkInfo, showNotification, ...rest } = this.props

    if (!item) {
      console.log("item is null, not rendering")
      return null
    }
    const props2 = { item, networkInfo, showNotification }

    const SpecificModal = XTransactionModal
    return (
      <Box {...rest}>
        <SpecificModal {...props2} />
      </Box>
    )
  }
}

export default XActivityModal