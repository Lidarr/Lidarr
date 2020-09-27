import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { grabQueueItem, removeQueueItem } from 'Store/Actions/queueActions';
import createAlbumSelector from 'Store/Selectors/createAlbumSelector';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import QueueRow from './QueueRow';

function createMapStateToProps() {
  return createSelector(
    createArtistSelector(),
    createAlbumSelector(),
    createUISettingsSelector(),
    (artist, album, uiSettings) => {
      const result = {
        showRelativeDates: uiSettings.showRelativeDates,
        shortDateFormat: uiSettings.shortDateFormat,
        timeFormat: uiSettings.timeFormat
      };

      result.artist = artist;
      result.album = album;

      return result;
    }
  );
}

const mapDispatchToProps = {
  grabQueueItem,
  removeQueueItem
};

class QueueRowConnector extends Component {

  //
  // Listeners

  onGrabPress = () => {
    this.props.grabQueueItem({ id: this.props.id });
  }

  onRemoveQueueItemPress = (payload) => {
    this.props.removeQueueItem({ id: this.props.id, ...payload });
  }

  //
  // Render

  render() {
    return (
      <QueueRow
        {...this.props}
        onGrabPress={this.onGrabPress}
        onRemoveQueueItemPress={this.onRemoveQueueItemPress}
      />
    );
  }
}

QueueRowConnector.propTypes = {
  id: PropTypes.number.isRequired,
  album: PropTypes.object,
  grabQueueItem: PropTypes.func.isRequired,
  removeQueueItem: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(QueueRowConnector);
