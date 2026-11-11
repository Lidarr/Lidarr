import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { toggleTracksMonitored } from 'Store/Actions/trackActions';
import { deleteTrackFile } from 'Store/Actions/trackFileActions';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createTrackFileSelector from 'Store/Selectors/createTrackFileSelector';
import { isCommandExecuting } from 'Utilities/Command';
import TrackRow from './TrackRow';

function createMapStateToProps() {
  return createSelector(
    (state, { id }) => id,
    createTrackFileSelector(),
    createCommandsSelector(),
    (id, trackFile, commands) => {
      const isSearching = commands.some((command) => {
        return command.name === commandNames.TRACK_SEARCH &&
          isCommandExecuting(command) &&
          command.body.trackIds.includes(id);
      });

      return {
        trackFilePath: trackFile ? trackFile.path : null,
        trackFileSize: trackFile ? trackFile.size : null,
        customFormats: trackFile ? trackFile.customFormats : [],
        customFormatScore: trackFile ? trackFile.customFormatScore : 0,
        indexerFlags: trackFile ? trackFile.indexerFlags : 0,
        isSearching
      };
    }
  );
}

const mapDispatchToProps = {
  deleteTrackFile,
  toggleTracksMonitored,
  executeCommand
};

export default connect(createMapStateToProps, mapDispatchToProps)(TrackRow);
