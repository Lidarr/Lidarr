import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createAllArtistSelector from './createAllArtistSelector';

function createArtistCountSelector() {
  return createSelector(
    createAllArtistSelector(),
    (state: AppState) => state.artist.error,
    (state: AppState) => state.artist.isFetching,
    (state: AppState) => state.artist.isPopulated,
    (artists, error, isFetching, isPopulated) => {
      return {
        count: artists.length,
        error,
        isFetching,
        isPopulated,
      };
    }
  );
}

export default createArtistCountSelector;
