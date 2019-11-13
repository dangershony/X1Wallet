import React from 'react'
import { Tabs } from '../renderer/components/UI'
import ConnectionDetailsContext from '../renderer/components/Onboarding/Steps/ConnectionDetailsContext'

class X1ConnectionDetailsTabs extends React.PureComponent {
  items = [
    {
      key: 'FORM_TYPE_OBSIDIAN',
      name: 'Obsidian',
    },
  ]

  render() {
    return (
      <ConnectionDetailsContext.Consumer>
        {({ formType, openModal }) => {
          return (
            <Tabs activeKey={formType} items={this.items} onClick={openModal} {...this.props} />
          )
        }}
      </ConnectionDetailsContext.Consumer>
    )
  }
}

export default X1ConnectionDetailsTabs
