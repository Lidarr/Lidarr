import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createQueueItemSelector() {
  return createSelector(
    (_: AppState, { albumId }: { albumId: number }) => albumId,
    (state: AppState) => state.queue.details.items,
    (albumId, details) => {
      if (!albumId || !details) {
        return null;
      }

      return details.find((item) => item.albumId === albumId);
    }
  );
}

export default createQueueItemSelector;
