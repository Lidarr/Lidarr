import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setAlbumsSort, setAlbumsTableOption, toggleAlbumsMonitored } from 'Store/Actions/albumActions';
import { executeCommand } from 'Store/Actions/commandActions';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import ArtistDetailsSeason from './ArtistDetailsSeason';

function createMapStateToProps() {
  return createSelector(
    (state, { label }) => label,
    createClientSideCollectionSelector('albums'),
    createArtistSelector(),
    createCommandsSelector(),
    createDimensionsSelector(),
    createUISettingsSelector(),
    (label, albums, artist, commands, dimensions, uiSettings) => {
      const albumsByType = albums.items.filter(({ albumType }) => albumType === label);

      return {
        items: albumsByType,
        columns: albums.columns,
        artistMonitored: artist.monitored,
        sortKey: albums.sortKey,
        sortDirection: albums.sortDirection,
        isSmallScreen: dimensions.isSmallScreen,
        uiSettings
      };
    }
  );
}

const mapDispatchToProps = {
  toggleAlbumsMonitored,
  setAlbumsTableOption,
  dispatchSetAlbumSort: setAlbumsSort,
  executeCommand
};

class ArtistDetailsSeasonConnector extends Component {

  //
  // Listeners

  onTableOptionChange = (payload) => {
    this.props.setAlbumsTableOption(payload);
  };

  onSortPress = (sortKey) => {
    this.props.dispatchSetAlbumSort({ sortKey });
  };

  onMonitorAlbumsPress = (albumIds, monitored) => {
    this.props.toggleAlbumsMonitored({
      albumIds,
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <ArtistDetailsSeason
        {...this.props}
        onSortPress={this.onSortPress}
        onTableOptionChange={this.onTableOptionChange}
        onMonitorAlbumsPress={this.onMonitorAlbumsPress}
      />
    );
  }
}

ArtistDetailsSeasonConnector.propTypes = {
  artistId: PropTypes.number.isRequired,
  toggleAlbumsMonitored: PropTypes.func.isRequired,
  setAlbumsTableOption: PropTypes.func.isRequired,
  dispatchSetAlbumSort: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ArtistDetailsSeasonConnector);
