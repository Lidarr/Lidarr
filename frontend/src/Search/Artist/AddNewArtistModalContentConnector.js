import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { addArtist, setAddDefault } from 'Store/Actions/searchActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import AddNewArtistModalContent from './AddNewArtistModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.search,
    (state) => state.settings.metadataProfiles,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (searchState, metadataProfiles, dimensions, systemStatus) => {
      const {
        isAdding,
        addError,
        defaults
      } = searchState;

      const {
        settings,
        validationErrors,
        validationWarnings
      } = selectSettings(defaults, {}, addError);

      return {
        isAdding,
        addError,
        showMetadataProfile: metadataProfiles.items.length > 2, // NONE (not allowed for artists) and one other
        isSmallScreen: dimensions.isSmallScreen,
        validationErrors,
        validationWarnings,
        isWindows: systemStatus.isWindows,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  setAddDefault,
  addArtist
};

class AddNewArtistModalContentConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setAddDefault({ [name]: value });
  };

  onAddArtistPress = () => {
    const {
      foreignArtistId,
      rootFolderPath,
      monitor,
      monitorNewItems,
      qualityProfileId,
      metadataProfileId,
      searchForMissingAlbums,
      tags
    } = this.props;

    this.props.addArtist({
      foreignArtistId,
      rootFolderPath: rootFolderPath.value,
      monitor: monitor.value,
      monitorNewItems: monitorNewItems.value,
      qualityProfileId: qualityProfileId.value,
      metadataProfileId: metadataProfileId.value,
      searchForMissingAlbums: searchForMissingAlbums.value,
      tags: tags.value
    });
  };

  //
  // Render

  render() {
    return (
      <AddNewArtistModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onAddArtistPress={this.onAddArtistPress}
      />
    );
  }
}

AddNewArtistModalContentConnector.propTypes = {
  foreignArtistId: PropTypes.string.isRequired,
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  monitorNewItems: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  metadataProfileId: PropTypes.object,
  searchForMissingAlbums: PropTypes.object.isRequired,
  tags: PropTypes.object.isRequired,
  onModalClose: PropTypes.func.isRequired,
  setAddDefault: PropTypes.func.isRequired,
  addArtist: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddNewArtistModalContentConnector);
