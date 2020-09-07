import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setAlbumStudioFilter } from 'Store/Actions/albumStudioActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.artist.items,
    (state) => state.albumStudio.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'albumStudio'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setAlbumStudioFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
