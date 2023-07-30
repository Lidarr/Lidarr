import { some } from 'lodash';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createAllArtistSelector from './createAllArtistSelector';

function createExistingArtistSelector() {
  return createSelector(
    (_: AppState, { foreignArtistId }: { foreignArtistId: string }) =>
      foreignArtistId,
    createAllArtistSelector(),
    (foreignArtistId, artist) => {
      return some(artist, { foreignArtistId });
    }
  );
}

export default createExistingArtistSelector;
