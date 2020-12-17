import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { saveArtistEditor, setArtistEditorFilter, setArtistEditorSort, setArtistEditorTableOption } from 'Store/Actions/artistEditorActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchRootFolders } from 'Store/Actions/settingsActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import ArtistEditor from './ArtistEditor';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('artist', 'artistEditor'),
    createCommandExecutingSelector(commandNames.RENAME_ARTIST),
    createCommandExecutingSelector(commandNames.RETAG_ARTIST),
    (artist, isOrganizingArtist, isRetaggingArtist) => {
      return {
        isOrganizingArtist,
        isRetaggingArtist,
        ...artist
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetArtistEditorSort: setArtistEditorSort,
  dispatchSetArtistEditorFilter: setArtistEditorFilter,
  dispatchSetArtistEditorTableOption: setArtistEditorTableOption,
  dispatchSaveArtistEditor: saveArtistEditor,
  dispatchFetchRootFolders: fetchRootFolders,
  dispatchExecuteCommand: executeCommand
};

class ArtistEditorConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchRootFolders();
  }

  //
  // Listeners

  onSortPress = (sortKey) => {
    this.props.dispatchSetArtistEditorSort({ sortKey });
  }

  onFilterSelect = (selectedFilterKey) => {
    this.props.dispatchSetArtistEditorFilter({ selectedFilterKey });
  }

  onTableOptionChange = (payload) => {
    this.props.dispatchSetArtistEditorTableOption(payload);
  }

  onSaveSelected = (payload) => {
    this.props.dispatchSaveArtistEditor(payload);
  }

  onMoveSelected = (payload) => {
    this.props.dispatchExecuteCommand({
      name: commandNames.MOVE_ARTIST,
      ...payload
    });
  }

  //
  // Render

  render() {
    return (
      <ArtistEditor
        {...this.props}
        onSortPress={this.onSortPress}
        onFilterSelect={this.onFilterSelect}
        onSaveSelected={this.onSaveSelected}
        onTableOptionChange={this.onTableOptionChange}
      />
    );
  }
}

ArtistEditorConnector.propTypes = {
  dispatchSetArtistEditorSort: PropTypes.func.isRequired,
  dispatchSetArtistEditorFilter: PropTypes.func.isRequired,
  dispatchSetArtistEditorTableOption: PropTypes.func.isRequired,
  dispatchSaveArtistEditor: PropTypes.func.isRequired,
  dispatchFetchRootFolders: PropTypes.func.isRequired,
  dispatchExecuteCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ArtistEditorConnector);
