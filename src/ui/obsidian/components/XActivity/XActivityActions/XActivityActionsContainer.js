import { connect } from 'react-redux'
import {
  changeFilter,
  xfetchActivityHistory,
  updateSearchText,
  fullNodeActivitySelectors,
} from '../../../reducers/fullNodeActivities'
import XActivityActions from './XActivityActions'

const mapDispatchToProps = {
  changeFilter,
  xfetchActivityHistory,
  updateSearchText: updateSearchText,
}

const mapStateToProps = state => {
  const props = ({
    filter: fullNodeActivitySelectors.filter(state),
    filters: fullNodeActivitySelectors.filters(state),
    searchText: fullNodeActivitySelectors.searchText(state),
  });
  return props
}

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(XActivityActions)
