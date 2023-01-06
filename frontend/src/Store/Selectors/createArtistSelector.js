import { createSelector } from 'reselect';

export function createArtistSelectorForHook(artistId) {
  return createSelector(
    (state) => state.artist.itemMap,
    (state) => state.artist.items,
    (itemMap, allArtists) => {
      return artistId ? allArtists[itemMap[artistId]]: undefined;
    }
  );
}

function createArtistSelector() {
  return createSelector(
    (state, { artistId }) => artistId,
    (state) => state.artist.itemMap,
    (state) => state.artist.items,
    (artistId, itemMap, allArtists) => {
      return allArtists[itemMap[artistId]];
    }
  );
}

export default createArtistSelector;
