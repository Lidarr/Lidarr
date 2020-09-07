import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ArtistNameLink from 'Artist/ArtistNameLink';
import ArtistStatusCell from 'Artist/Index/Table/ArtistStatusCell';
import CheckInput from 'Components/Form/CheckInput';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import TagListConnector from 'Components/TagListConnector';
import styles from './ArtistEditorRow.css';

class ArtistEditorRow extends Component {

  //
  // Listeners

  onAlbumFolderChange = () => {
    // Mock handler to satisfy `onChange` being required for `CheckInput`.
    //
  }

  //
  // Render

  render() {
    const {
      id,
      status,
      foreignArtistId,
      artistName,
      artistType,
      monitored,
      metadataProfile,
      qualityProfile,
      albumFolder,
      path,
      tags,
      columns,
      isSaving,
      isSelected,
      onArtistMonitoredPress,
      onSelectedChange
    } = this.props;

    return (
      <TableRow>
        <TableSelectCell
          id={id}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        <ArtistStatusCell
          artistType={artistType}
          monitored={monitored}
          status={status}
          isSaving={isSaving}
          onMonitoredPress={onArtistMonitoredPress}
        />

        <TableRowCell className={styles.title}>
          <ArtistNameLink
            foreignArtistId={foreignArtistId}
            artistName={artistName}
          />
        </TableRowCell>

        <TableRowCell>
          {qualityProfile.name}
        </TableRowCell>

        {
          _.find(columns, { name: 'metadataProfileId' }).isVisible &&
            <TableRowCell>
              {metadataProfile.name}
            </TableRowCell>
        }

        <TableRowCell className={styles.albumFolder}>
          <CheckInput
            name="albumFolder"
            value={albumFolder}
            isDisabled={true}
            onChange={this.onAlbumFolderChange}
          />
        </TableRowCell>

        <TableRowCell>
          {path}
        </TableRowCell>

        <TableRowCell>
          <TagListConnector
            tags={tags}
          />
        </TableRowCell>
      </TableRow>
    );
  }
}

ArtistEditorRow.propTypes = {
  id: PropTypes.number.isRequired,
  status: PropTypes.string.isRequired,
  foreignArtistId: PropTypes.string.isRequired,
  artistName: PropTypes.string.isRequired,
  artistType: PropTypes.string,
  monitored: PropTypes.bool.isRequired,
  metadataProfile: PropTypes.object.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  albumFolder: PropTypes.bool.isRequired,
  path: PropTypes.string.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onArtistMonitoredPress: PropTypes.func.isRequired,
  onSelectedChange: PropTypes.func.isRequired
};

ArtistEditorRow.defaultProps = {
  tags: []
};

export default ArtistEditorRow;
