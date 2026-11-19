import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setTracksTableOption, toggleTracksMonitored } from 'Store/Actions/trackActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import AlbumDetailsMedium from './AlbumDetailsMedium';

function createMapStateToProps() {
  return createSelector(
    (state, { mediumNumber }) => mediumNumber,
    (state) => state.tracks,
    createDimensionsSelector(),
    (mediumNumber, tracks, dimensions) => {

      const tracksInMedium = _.filter(tracks.items, { mediumNumber });
      const sortedTracks = _.orderBy(tracksInMedium, ['absoluteTrackNumber'], ['asc']);

      return {
        items: sortedTracks,
        columns: tracks.columns,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  setTracksTableOption,
  toggleTracksMonitored
};

class AlbumDetailsMediumConnector extends Component {

  //
  // Listeners

  onTableOptionChange = (payload) => {
    this.props.setTracksTableOption(payload);
  };

  onTracksMonitoredChange = (trackIds, monitored) => {
    this.props.toggleTracksMonitored({ trackIds, monitored });
  };

  //
  // Render

  render() {
    return (
      <AlbumDetailsMedium
        {...this.props}
        onTableOptionChange={this.onTableOptionChange}
        onTracksMonitoredChange={this.onTracksMonitoredChange}
      />
    );
  }
}

AlbumDetailsMediumConnector.propTypes = {
  albumId: PropTypes.number.isRequired,
  albumTitle: PropTypes.string.isRequired,
  albumMonitored: PropTypes.bool.isRequired,
  albumReleaseDate: PropTypes.string,
  mediumNumber: PropTypes.number.isRequired,
  setTracksTableOption: PropTypes.func.isRequired,
  toggleTracksMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AlbumDetailsMediumConnector);
