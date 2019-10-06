import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAlbumSelector from 'Store/Selectors/createAlbumSelector';
import createQueueItemSelector from 'Store/Selectors/createQueueItemSelector';
// import createTrackFileSelector from 'Store/Selectors/createTrackFileSelector';
import AlbumStatus from './AlbumStatus';

function createMapStateToProps() {
  return createSelector(
    createAlbumSelector(),
    createQueueItemSelector(),
    (album, queueItem) => {
      const result = _.pick(album, [
        'releaseDate',
        'monitored',
        'grabbed'
      ]);

      result.queueItem = queueItem;

      return result;
    }
  );
}

const mapDispatchToProps = {
};

class AlbumStatusConnector extends Component {

  //
  // Render

  render() {
    return (
      <AlbumStatus
        {...this.props}
      />
    );
  }
}

AlbumStatusConnector.propTypes = {
  albumId: PropTypes.number.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AlbumStatusConnector);
