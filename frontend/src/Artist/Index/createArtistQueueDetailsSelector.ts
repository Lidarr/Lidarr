import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

export interface ArtistQueueDetails {
  count: number;
  tracksWithFiles: number;
}

function createArtistQueueDetailsSelector(artistId: number) {
  return createSelector(
    (state: AppState) => state.queue.details.items,
    (queueItems) => {
      return queueItems.reduce(
        (acc: ArtistQueueDetails, item) => {
          if (
            item.trackedDownloadState === 'imported' ||
            item.artistId !== artistId
          ) {
            return acc;
          }

          acc.count += item.trackFileCount ?? 0;

          if (item.trackHasFileCount) {
            acc.tracksWithFiles += item.trackHasFileCount;
          }

          return acc;
        },
        {
          count: 0,
          tracksWithFiles: 0,
        }
      );
    }
  );
}

export default createArtistQueueDetailsSelector;
