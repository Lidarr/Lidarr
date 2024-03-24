import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Artist from 'Artist/Artist';

function createMultiArtistsSelector(artistIds: number[]) {
  return createSelector(
    (state: AppState) => state.artist.itemMap,
    (state: AppState) => state.artist.items,
    (itemMap, allArtists) => {
      return artistIds.reduce((acc: Artist[], artistId) => {
        const artist = allArtists[itemMap[artistId]];

        if (artist) {
          acc.push(artist);
        }

        return acc;
      }, []);
    }
  );
}

export default createMultiArtistsSelector;
