import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createTrackFileSelector() {
  return createSelector(
    (_: AppState, { trackFileId }: { trackFileId: number }) => trackFileId,
    (state: AppState) => state.trackFiles,
    (trackFileId, trackFiles) => {
      if (!trackFileId) {
        return;
      }

      return trackFiles.items.find((trackFile) => trackFile.id === trackFileId);
    }
  );
}

export default createTrackFileSelector;
