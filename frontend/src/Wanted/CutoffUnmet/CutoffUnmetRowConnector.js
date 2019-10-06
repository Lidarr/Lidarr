import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import createAlbumTrackFilesSelector from 'Store/Selectors/createAlbumTrackFilesSelector';
import CutoffUnmetRow from './CutoffUnmetRow';

function createMapStateToProps() {
  return createSelector(
    createArtistSelector(),
    createAlbumTrackFilesSelector(),
    (artist, trackFiles) => {
      return {
        artist,
        trackFiles
      };
    }
  );
}

export default connect(createMapStateToProps)(CutoffUnmetRow);
