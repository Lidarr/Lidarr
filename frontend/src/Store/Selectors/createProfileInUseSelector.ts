import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Artist from 'Artist/Artist';
import ImportList from 'typings/ImportList';
import createAllArtistSelector from './createAllArtistSelector';

function createProfileInUseSelector(profileProp: string) {
  return createSelector(
    (_: AppState, { id }: { id: number }) => id,
    createAllArtistSelector(),
    (state: AppState) => state.settings.importLists.items,
    (id, artists, lists) => {
      if (!id) {
        return false;
      }

      return (
        artists.some((a) => a[profileProp as keyof Artist] === id) ||
        lists.some((list) => list[profileProp as keyof ImportList] === id)
      );
    }
  );
}

export default createProfileInUseSelector;
