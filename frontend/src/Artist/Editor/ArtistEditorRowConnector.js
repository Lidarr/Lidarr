import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleArtistMonitored } from 'Store/Actions/artistActions';
import createMetadataProfileSelector from 'Store/Selectors/createMetadataProfileSelector';
import createQualityProfileSelector from 'Store/Selectors/createQualityProfileSelector';
import ArtistEditorRow from './ArtistEditorRow';

function createMapStateToProps() {
  return createSelector(
    createMetadataProfileSelector(),
    createQualityProfileSelector(),
    (metadataProfile, qualityProfile) => {
      return {
        metadataProfile,
        qualityProfile
      };
    }
  );
}

const mapDispatchToProps = {
  toggleArtistMonitored
};

class ArtistEditorRowConnector extends Component {

  //
  // Listeners

  onArtistMonitoredPress = () => {
    const {
      id,
      monitored
    } = this.props;

    this.props.toggleArtistMonitored({
      artistId: id,
      monitored: !monitored
    });
  }

  render() {
    return (
      <ArtistEditorRow
        {...this.props}
        onArtistMonitoredPress={this.onArtistMonitoredPress}
      />
    );
  }
}

ArtistEditorRowConnector.propTypes = {
  id: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  qualityProfileId: PropTypes.number.isRequired,
  toggleArtistMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ArtistEditorRowConnector);
