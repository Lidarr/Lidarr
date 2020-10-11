import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { removeBlacklistItem } from 'Store/Actions/blacklistActions';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import BlacklistRow from './BlacklistRow';

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
      dispatch(removeBlacklistItem({ id: props.id }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(BlacklistRow);
