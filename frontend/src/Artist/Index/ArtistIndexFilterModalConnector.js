import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setArtistFilter } from 'Store/Actions/artistIndexActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.artist.items,
    (state) => state.artistIndex.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'artistIndex'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setArtistFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
