import React from 'react'
import { List, AutoSizer, CellMeasurer, CellMeasurerCache } from 'react-virtualized'
import styled from 'styled-components'
import { space } from 'styled-system'
import PropTypes from 'prop-types'
import get from 'lodash/get'
import {
  FormattedDate,
  FormattedTime,
  FormattedMessage,
  FormattedNumber,
  injectIntl,
  intlShape,
} from 'react-intl'
import { Flex, Box } from 'rebass'
import blockExplorer from '@zap/utils/blockExplorer'
import { Bar, DataRow, Header, Link, Panel, Span, Text } from '../../../../renderer/components/UI'
import {
  CopyButton,
  CryptoValue,
  FiatSelector,
  FiatValue,
} from '../../../../renderer/containers/UI'
import { Truncate } from '../../../../renderer/components/Util'
import Onchain from '../../../../renderer/components/Icon/Onchain'
import Padlock from '../../../../renderer/components/Icon/Padlock'
import xmessages from './xmessages'
import CryptoSelector from '../../XWallet/XCryptoSelector'
import XRecipientListItem from './XRecipientListItem'

const StyledList = styled(List)`
  ${space}
  outline: none;
  padding-left: 12px;
`

class XTransactionModal extends React.PureComponent {
  static propTypes = {
    intl: intlShape.isRequired,
    item: PropTypes.object.isRequired,
    networkInfo: PropTypes.shape({
      id: PropTypes.string,
      name: PropTypes.string,
    }),
    showNotification: PropTypes.func.isRequired,
  }
  cache = new CellMeasurerCache({
    fixedWidth: true,
    minHeight: 52,
  })

  componentDidUpdate() {
    // update list since item heights might have changed
    this.updateList()
  }

  updateList = () => {
    this.cache.clearAll()
    this._list && this._list.recomputeRowHeights(0)
  }

  onListResize = ({ width }) => {
    // only invalidate row measurement cache if width has actually changed
    if (this._prevListWidth != width) {
      this.updateList()
    }
    this._prevListWidth = width
  }

  showBlock = hash => {
    const { networkInfo } = this.props
    return networkInfo && blockExplorer.showBlock(networkInfo, hash)
  }

  showAddress = address => {
    const { networkInfo } = this.props
    return networkInfo && blockExplorer.showAddress(networkInfo, address)
  }

  blockExplorerShowTransaction = (network, txid) => {
    window.Zap.openExternal(`${network.explorerUrl}/fetchtransaction/${txid}`)
  }

  showTransaction = hash => {
    const { networkInfo } = this.props
    return networkInfo && this.blockExplorerShowTransaction(networkInfo, hash)
  }

  renderDestinations = recipients => {
    if(!recipients)
    {
      return null
    }
    console.log('rendering ' + recipients.length + ' recipients')

    const renderRow = ({ index, key, style, parent }) => {
      const item = recipients[index]
      return (
        <CellMeasurer key={key} cache={this.cache} columnIndex={0} parent={parent} rowIndex={index}>
          <div style={style}>
            {item && item.title ? (
              <Box mt={4} pl={4}>
                <Heading.h4 fontWeight="normal">
                  <FormattedDate day="2-digit" month="short" value={item.title} year="numeric" />
                </Heading.h4>
                <Bar my={1} />
              </Box>
            ) : (
              <XRecipientListItem recipient={recipients[index]} />
            )}
          </div>
        </CellMeasurer>
      )
    }
    return (
      <AutoSizer onResize={this.onListResize}>
        {({ width, height }) => {
          return (
            <StyledList
              ref={ref => (this._list = ref)}
              deferredMeasurementCache={this.cache}
              height={height}
              pr={4}
              rowCount={recipients.length}
              rowHeight={this.cache.rowHeight}
              rowRenderer={renderRow}
              width={width}
            />
          )
        }}
      </AutoSizer>
    )
  }

  render() {
    const { intl, item, showNotification, cryptoUnitName, ...rest } = this.props
    const destAddress = get(item, 'dest_addresses[0]')
    const amount = item.amount || item.limboAmount || 0
    const isIncoming = item.received || item.limboAmount > 0
    const {
      txType,
      valueAdded,
      totalSpent,
      totalReceived,
      hashTx,
      heightBlock,
      timeBlock,
      hashBlock,
      recipients,
    } = item
    
    const fee = totalReceived - totalSpent +valueAdded
    const confirmations = 1
    const fullNodeCryptoUnitName = window.x1Tools.translateCryptoUnitName(cryptoUnitName)
    return (
      <Panel {...rest}>
        <Panel.Header>
          <Header
            logo={<Onchain height="45px" width="45px" />}
            subtitle={<FormattedMessage {...xmessages.subtitle} />}
            title={<FormattedMessage {...xmessages[txType]} />}
          />
          <Bar mt={2} />
        </Panel.Header>

        <Panel.Body>
          <DataRow
            left={<FormattedMessage {...xmessages.amount} />}
            right={
              <Flex alignItems="center">
                <CryptoSelector mr={2} />
                <CryptoValue fontSize="xxl" value={valueAdded} />
              </Flex>
            }
          />

          <Bar variant="light" />

          <DataRow
            left={<FormattedMessage {...xmessages.current_value} />}
            right={
              <Flex alignItems="center">
                <FiatSelector mr={2} />
                <FiatValue value={valueAdded} />
              </Flex>
            }
          />

          {confirmations > 0 && (
            <>
              <Bar variant="light" />

              <DataRow
                left={<FormattedMessage {...xmessages.date_confirmed} />}
                right={
                  confirmations ? (
                    <>
                      <Text>
                        <FormattedDate
                          day="2-digit"
                          month="long"
                          value={timeBlock * 1000}
                          year="numeric"
                        />
                      </Text>
                      <Text>
                        <FormattedTime value={timeBlock * 1000} />
                      </Text>
                    </>
                  ) : (
                    <FormattedMessage {...xmessages.unconfirmed} />
                  )
                }
              />

              <Bar variant="light" />

              <DataRow
                left={<FormattedMessage {...xmessages.num_confirmations} />}
                right={<FormattedNumber value={confirmations} />}
              />

              <Bar variant="light" />
              <div style={{ height: 100, maxHeight: 200 }}>
                {this.renderDestinations(recipients)}
              </div>
              <DataRow
                left={<FormattedMessage {...xmessages.address} />}
                right={
                  <Flex>
                    <CopyButton
                      mr={2}
                      name={intl.formatMessage({ ...xmessages.address })}
                      size="0.7em"
                      value={destAddress}
                    />
                    <Link
                      className="hint--bottom-left"
                      data-hint={destAddress}
                      onClick={() => this.showAddress(destAddress)}
                    >
                      <Truncate text={destAddress} />
                    </Link>
                  </Flex>
                }
              />
            </>
          )}

          {!isIncoming && (
            <>
              <Bar variant="light" />

              <DataRow
                left={<FormattedMessage {...xmessages.fee} />}
                right={
                  <Flex 
                  alignItems="center"
                  justifyContent="space-between"
                  py={2}
                  >
                    <Box
                      className="hint--top-left"
                      data-hint={intl.formatMessage({ ...xmessages.amount })}
                      width={1}
                    >
                      <Box css={{ opacity: 1 }}>
                        <Text color={amount >= 0 ? 'superGreen' : null} mb={1} textAlign="right">
                          {fee >= 0 ? `+ ` : `- `}
                          <CryptoValue value={fee} />
                          <i> {fullNodeCryptoUnitName}</i>
                        </Text>
                        <Text color="gray" fontSize="xs" fontWeight="normal" textAlign="right">
                          <FiatValue style="currency" value={fee} />
                        </Text>
                      </Box>
                    </Box>
                  </Flex>
                }
              />
            </>
          )}

          <Bar variant="light" />
          <DataRow
            left={<FormattedMessage {...xmessages.status} />}
            right={
              item.block_height ? (
                <>
                  <Flex>
                    <CopyButton
                      mr={2}
                      name={intl.formatMessage({ ...xmessages.block_id })}
                      size="0.7em"
                      value={item.block_hash}
                    />
                    <Link
                      className="hint--bottom-left"
                      data-hint={item.block_hash}
                      onClick={() => this.showBlock(item.block_hash)}
                    >
                      <FormattedMessage
                        {...xmessages.block_height}
                        values={{ height: item.block_height }}
                      />
                    </Link>
                  </Flex>

                  {item.maturityHeight && (
                    <Flex alignItems="center" mt={1}>
                      <Span color="gray" fontSize="s" mr={1}>
                        <Padlock />
                      </Span>
                      <Text>
                        <FormattedMessage
                          {...xmessages.maturity_height}
                          values={{ height: item.maturityHeight }}
                        />
                      </Text>
                    </Flex>
                  )}
                </>
              ) : (
                <FormattedMessage {...xmessages.unconfirmed} />
              )
            }
          />

          <Bar variant="light" />

          <DataRow
            left={<FormattedMessage {...xmessages.tx_hash} />}
            right={
              <Flex>
                <CopyButton
                  mr={2}
                  name={intl.formatMessage({ ...xmessages.tx_hash })}
                  size="0.7em"
                  value={hashTx}
                />
                <Link
                  className="hint--bottom-left"
                  data-hint={item.tx_hash}
                  onClick={() => this.showTransaction(hashTx)}
                >
                  <Truncate text={hashTx} />
                </Link>
              </Flex>
            }
          />

          {/* <Panel>
            <Panel.Header my={3} px={4}>
              <div>Header</div>
            </Panel.Header>
            <Panel.Body>{this.renderDestinations(recipients)}</Panel.Body>
          </Panel> */}
        </Panel.Body>
      </Panel>
    )
  }
}

export default injectIntl(XTransactionModal)
