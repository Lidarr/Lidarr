import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { removeBlocklistItem } from 'Store/Actions/blocklistActions';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import BlocklistRow from './BlocklistRow';

function createMapStateToProps() {
  return createSelector(
    createArtistSelector(),
    (artist) => {
      return {
        artist
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onRemovePress() {
      dispatch(removeBlocklistItem({ id: props.id }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(BlocklistRow);
