import { createSelector } from 'reselect';

function createAlbumTrackFilesSelector() {
  return createSelector(
    (state, { albumId }) => albumId,
    (state) => state.trackFiles(),
    (albumId, trackFiles) => {
      return trackFiles.find((trackFile) => trackFile.albumId === albumId );
    }
  );
}

export default createAlbumTrackFilesSelector;
