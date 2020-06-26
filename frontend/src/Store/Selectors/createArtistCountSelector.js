import { createSelector } from 'reselect';
import createAllArtistSelector from './createAllArtistSelector';

function createArtistCountSelector() {
  return createSelector(
    createAllArtistSelector(),
    (state) => state.artist.error,
    (state) => state.artist.isFetching,
    (state) => state.artist.isPopulated,
    (artists, error, isFetching, isPopulated) => {
      return {
        count: artists.length,
        error,
        isFetching,
        isPopulated
      };
    }
  );
}

export default createArtistCountSelector;
