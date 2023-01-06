import { createSelector } from 'reselect';
import Artist from 'Artist/Artist';
import { ARTIST_SEARCH, REFRESH_ARTIST } from 'Commands/commandNames';
import createArtistMetadataProfileSelector from 'Store/Selectors/createArtistMetadataProfileSelector';
import createArtistQualityProfileSelector from 'Store/Selectors/createArtistQualityProfileSelector';
import { createArtistSelectorForHook } from 'Store/Selectors/createArtistSelector';
import createExecutingCommandsSelector from 'Store/Selectors/createExecutingCommandsSelector';

function createArtistIndexItemSelector(artistId: number) {
  return createSelector(
    createArtistSelectorForHook(artistId),
    createArtistQualityProfileSelector(artistId),
    createArtistMetadataProfileSelector(artistId),
    createExecutingCommandsSelector(),
    (artist: Artist, qualityProfile, metadataProfile, executingCommands) => {
      // If an artist is deleted this selector may fire before the parent
      // selectors, which will result in an undefined artist, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show an artist that has no information available.

      if (!artist) {
        return {};
      }

      const isRefreshingArtist = executingCommands.some((command) => {
        return (
          command.name === REFRESH_ARTIST && command.body.artistId === artist.id
        );
      });

      const isSearchingArtist = executingCommands.some((command) => {
        return (
          command.name === ARTIST_SEARCH && command.body.artistId === artist.id
        );
      });

      return {
        artist,
        qualityProfile,
        metadataProfile,
        isRefreshingArtist,
        isSearchingArtist,
      };
    }
  );
}

export default createArtistIndexItemSelector;
