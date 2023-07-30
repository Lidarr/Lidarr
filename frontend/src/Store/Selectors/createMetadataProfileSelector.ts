import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createMetadataProfileSelector() {
  return createSelector(
    (_: AppState, { metadataProfileId }: { metadataProfileId: number }) =>
      metadataProfileId,
    (state: AppState) => state.settings.metadataProfiles.items,
    (metadataProfileId, metadataProfiles) => {
      return metadataProfiles.find(
        (profile) => profile.id === metadataProfileId
      );
    }
  );
}

export default createMetadataProfileSelector;
