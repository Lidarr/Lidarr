import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createAllArtistSelector() {
  return createSelector(
    (state: AppState) => state.artist,
    (artist) => {
      return artist.items;
    }
  );
}

export default createAllArtistSelector;
