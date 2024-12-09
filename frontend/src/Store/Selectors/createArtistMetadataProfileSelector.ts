import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Artist from 'Artist/Artist';
import MetadataProfile from 'typings/MetadataProfile';
import { createArtistSelectorForHook } from './createArtistSelector';

function createArtistMetadataProfileSelector(artistId: number) {
  return createSelector(
    (state: AppState) => state.settings.metadataProfiles.items,
    createArtistSelectorForHook(artistId),
    (metadataProfiles: MetadataProfile[], artist = {} as Artist) => {
      return metadataProfiles.find((profile) => {
        return profile.id === artist.metadataProfileId;
      });
    }
  );
}

export default createArtistMetadataProfileSelector;
