/* eslint max-params: 0 */
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import createTrackFileSelector from 'Store/Selectors/createTrackFileSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import AlbumRow from './AlbumRow';

function createMapStateToProps() {
  return createSelector(
    (state, { id }) => id,
    (state, { sceneSeasonNumber }) => sceneSeasonNumber,
    createArtistSelector(),
    createTrackFileSelector(),
    createCommandsSelector(),
    (id, sceneSeasonNumber, artist, trackFile, commands) => {
      /* eslint-disable no-undef */
      const alternateTitles = sceneSeasonNumber ? _.filter(artist.alternateTitles, { sceneSeasonNumber }) : [];
      /* eslint-enable no-undef */

      return {
        artistMonitored: artist.monitored,
        trackFilePath: trackFile ? trackFile.path : null,
        trackFileRelativePath: trackFile ? trackFile.relativePath : null,
        alternateTitles
      };
    }
  );
}
export default connect(createMapStateToProps)(AlbumRow);
