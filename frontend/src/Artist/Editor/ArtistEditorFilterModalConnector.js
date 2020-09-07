import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setArtistEditorFilter } from 'Store/Actions/artistEditorActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.artist.items,
    (state) => state.artistEditor.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'artistEditor'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setArtistEditorFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
