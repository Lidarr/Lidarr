import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchHistory, markAsFailed } from 'Store/Actions/historyActions';
import createAlbumSelector from 'Store/Selectors/createAlbumSelector';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import ArtistHistoryRow from './ArtistHistoryRow';

function createMapStateToProps() {
  return createSelector(
    createArtistSelector(),
    createAlbumSelector(),
    (artist, album) => {
      return {
        artist,
        album
      };
    }
  );
}

const mapDispatchToProps = {
  fetchHistory,
  markAsFailed
};

export default connect(createMapStateToProps, mapDispatchToProps)(ArtistHistoryRow);
