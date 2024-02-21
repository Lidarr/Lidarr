import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteTrackFile } from 'Store/Actions/trackFileActions';
import createTrackFileSelector from 'Store/Selectors/createTrackFileSelector';
import TrackRow from './TrackRow';

function createMapStateToProps() {
  return createSelector(
    (state, { id }) => id,
    createTrackFileSelector(),
    (id, trackFile) => {
      return {
        trackFilePath: trackFile ? trackFile.path : null,
        trackFileSize: trackFile ? trackFile.size : null,
        customFormats: trackFile ? trackFile.customFormats : [],
        customFormatScore: trackFile ? trackFile.customFormatScore : 0,
        indexerFlags: trackFile ? trackFile.indexerFlags : 0
      };
    }
  );
}

const mapDispatchToProps = {
  deleteTrackFile
};

export default connect(createMapStateToProps, mapDispatchToProps)(TrackRow);
