// See https://github.com/yahoo/react-intl/wiki/Testing-with-React-Intl#enzyme

/**
 * Components using the react-intl module require access to the intl context.
 * This is not available when mounting single components in Enzyme.
 * These helper functions aim to address that and wrap a valid,
 * English-locale intl context around them.
 */

import React from 'react'
import { IntlProvider, intlShape } from 'react-intl'
import { mount, shallow } from 'enzyme'

// Ppass default messages to the IntlProvider.
import messages from '@zap/translations/en.json'

// Create the IntlProvider to retrieve context for wrapping around.
const intlProvider = new IntlProvider({ locale: 'en', messages }, {})
const { intl } = intlProvider.getChildContext()

// nodeWithIntlProp - When using React-Intl `injectIntl` on components, props.intl is required.
const nodeWithIntlProp = node => {
  return React.cloneElement(node, { intl })
}

export const shallowWithIntl = (node, { context, ...additionalOptions } = {}) => {
  return shallow(nodeWithIntlProp(node), {
    context: Object.assign({}, context, { intl }),
    ...additionalOptions,
  })
}

export const mountWithIntl = (node, { context, childContextTypes, ...additionalOptions } = {}) => {
  return mount(nodeWithIntlProp(node), {
    context: Object.assign({}, context, { intl }),
    childContextTypes: Object.assign({}, { intl: intlShape }, childContextTypes),
    ...additionalOptions,
  })
}
