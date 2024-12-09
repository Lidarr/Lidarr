import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Artist from 'Artist/Artist';
import QualityProfile from 'typings/QualityProfile';
import { createArtistSelectorForHook } from './createArtistSelector';

function createArtistQualityProfileSelector(artistId: number) {
  return createSelector(
    (state: AppState) => state.settings.qualityProfiles.items,
    createArtistSelectorForHook(artistId),
    (qualityProfiles: QualityProfile[], artist = {} as Artist) => {
      return qualityProfiles.find(
        (profile) => profile.id === artist.qualityProfileId
      );
    }
  );
}

export default createArtistQualityProfileSelector;
