import { createSelector } from 'reselect';
import Artist from 'Artist/Artist';
import Command from 'Commands/Command';
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
    (
      artist: Artist,
      qualityProfile,
      metadataProfile,
      executingCommands: Command[]
    ) => {
      const isRefreshingArtist = executingCommands.some((command) => {
        return (
          command.name === REFRESH_ARTIST && command.body.artistId === artistId
        );
      });

      const isSearchingArtist = executingCommands.some((command) => {
        return (
          command.name === ARTIST_SEARCH && command.body.artistId === artistId
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
