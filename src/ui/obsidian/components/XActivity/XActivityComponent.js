import React, { Component } from 'react'
import PropTypes from 'prop-types'
import styled from 'styled-components'
import { space } from 'styled-system'
import { List, AutoSizer, CellMeasurer, CellMeasurerCache } from 'react-virtualized'
import { FormattedDate } from 'react-intl'
import { Box, Flex } from 'rebass'
import { Bar, Heading, Panel } from '../../../renderer/components/UI'
import XActivityActionsContainer from './XActivityActions/XActivityActionsContainer'
import XActivityListItem from './XActivityListItem'
import { PANE_NODEINFO, PANE_STAKINGINFO, PANE_GENERALINFO, PANE_IMPORTKEYS, PANE_EXPORTKEYS, PANE_RESCAN, DEFAULT_PANE } from './../XProfile/xconstants'

const StyledList = styled(List)`
  ${space}
  outline: none;
  padding-left: 12px;
`

class XActivityComponent extends Component {
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

  getBarStatus = (barNo, connections) => {
    let bars
    const off = '#555'
    const on = '#fff'
    if (!connections) {
      bars = 0
    } else if (connections === 1) {
      bars = 1
    } else if (connections < 3) {
      bars = 2
    } else if (connections < 6) {
      bars = 3
    } else if (connections < 9) {
      bars = 4
    } else {
      bars = 5
    }
    if (barNo <= bars) {
      return on
    }
    return off
  }

  renderActivityList = () => {
    const { blocks } = this.props.historyInfo
    const { consensusTipHeight } = this.props
    const currentActivity = []

    let previousDay = -1
    for (var b = 0; b < blocks.length; b++) {
      const block = blocks[b]
      const blockTime = new Date(block.time * 1000) // UTC
      const day = blockTime.getDay() // gets the day of the week, using local time
      if (day !== previousDay) {
        currentActivity.push({ title: blockTime })
        previousDay = day
      }

      for (var t = 0; t < block.transactions.length; t++) {
        const tx = block.transactions[t]

        const item = {
          txType: tx.txType,
          valueAdded: tx.valueAdded,
          totalSpent: tx.totalSpent,
          totalReceived: tx.totalReceived,
          hashTx: tx.hashTx,
          heightBlock: block.height,
          timeBlock: block.time,
          hashBlock: block.hashBlock,
          recipients: tx.recipients,
          confirmations: consensusTipHeight - block.height + 1,
        }
        currentActivity.push(item)
      }
    }

    const renderRow = ({ index, key, style, parent }) => {
      const item = currentActivity[index]
      return (
        <CellMeasurer key={key} cache={this.cache} columnIndex={0} parent={parent} rowIndex={index}>
          <div style={style}>
            {item.title ? (
              <Box mt={4} pl={4}>
                <Heading.h4 fontWeight="normal">
                  <FormattedDate day="2-digit" month="short" value={item.title} year="numeric" />
                </Heading.h4>
                <Bar my={1} />
              </Box>
            ) : (
              <XActivityListItem activity={currentActivity[index]} />
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
              rowCount={currentActivity.length}
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
    let {outBound, inBound} = this.props.connectionInfo
    const connections = outBound + inBound
    const float = 'left'
    const to = 'bottom'
    const fontSize = 12
    const marginTop = '5px'
    const barChar = '❙' // ❘ ❙ ❚
    const bar1 = {
      fontSize: fontSize,
      webkitTransform: 'scale(1,0.5) translate(0px,-0.2px)',
      float: float,
      transformOrigin: to,
      marginLeft: '5px',
      marginTop: marginTop,
      color: this.getBarStatus(1, connections),
    }
    const bar2 = {
      fontSize: fontSize,
      webkitTransform: 'scale(1,0.7) translate(0px,0.3px)',
      float: float,
      transformOrigin: to,
      marginTop: marginTop,
      color: this.getBarStatus(2, connections),
    }
    const bar3 = {
      fontSize: fontSize,
      webkitTransform: 'scale(1,1.0) translate(0px,0.6px)',
      float: float,
      transformOrigin: to,
      marginTop: marginTop,
      color: this.getBarStatus(3, connections),
    }
    const bar4 = {
      fontSize: fontSize,
      webkitTransform: 'scale(1,1.2) translate(0px,0.9px)',
      float: float,
      transformOrigin: to,
      marginTop: marginTop,
      color: this.getBarStatus(4, connections),
    }
    const bar5 = {
      fontSize: fontSize,
      webkitTransform: 'scale(1,1.4) translate(0px,1.1px)',
      float: float,
      transformOrigin: to,
      marginTop: marginTop,
      color: this.getBarStatus(5, connections),
    }
    const barsLabel = {
      fontSize: 11,
      float: float,
      marginTop: '6px',
      marginLeft: '4px',
      color: '#ddd',
    }
    const txList = (
      <Panel>
        <Panel.Header my={3} px={4}>
          <XActivityActionsContainer />
        </Panel.Header>
        <Panel.Body>{this.renderActivityList()}</Panel.Body>
        <Panel.Footer height={20} p={3}>
        <Flex alignItems="center" flexDirection="row" justifyContent="center">
          <Box onClick={()=> this.props.openModal('XPROFILE_MODAL',{ pane: PANE_NODEINFO})}
          style={{cursor: 'pointer'}}
            className="hint--top-left"
            data-hint={`Network:\u00A0${connections}\u00A0Connections`}
            width={1 / 12}
          >
            
            {outBound ? (<div style={barsLabel}>{outBound}⭎</div>): null /** ⬈ ⬊ 🡕 🡖 🠁 🠃  🡑 🡓 ⭎ ⭏*/}
            <div style={bar1}>{barChar}</div>
            <div style={bar2}>{barChar}</div>
            <div style={bar3}>{barChar}</div>
            <div style={bar4}>{barChar}</div>
            <div style={bar5}>{barChar}</div>
            {inBound ? (<div style={barsLabel}>{inBound}⭏</div>): null}
            
          </Box>
          </Flex>
        </Panel.Footer>
      </Panel>
    )
    return txList
  }
}

XActivityComponent.propTypes = {
  currentActivity: PropTypes.array.isRequired,
}

export default XActivityComponent
