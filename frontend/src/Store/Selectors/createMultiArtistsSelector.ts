import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createMultiArtistsSelector(artistIds: number[]) {
  return createSelector(
    (state: AppState) => state.artist.itemMap,
    (state: AppState) => state.artist.items,
    (itemMap, allArtists) => {
      return artistIds.map((artistId) => allArtists[itemMap[artistId]]);
    }
  );
}

export default createMultiArtistsSelector;
