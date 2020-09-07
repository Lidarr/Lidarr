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
        trackFilePath: trackFile ? trackFile.path : null
      };
    }
  );
}

const mapDispatchToProps = {
  deleteTrackFile
};

export default connect(createMapStateToProps, mapDispatchToProps)(TrackRow);
