import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import { deleteBlacklist } from 'Store/Actions/blacklistActions';
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

const mapStateToProps = {
  onDeletePress: deleteBlacklist
};

export default connect(createMapStateToProps, mapStateToProps)(BlacklistRow);
