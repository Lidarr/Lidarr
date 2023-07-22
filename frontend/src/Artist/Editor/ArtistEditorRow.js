import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ArtistNameLink from 'Artist/ArtistNameLink';
import ArtistStatusCell from 'Artist/Index/Table/ArtistStatusCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import TagListConnector from 'Components/TagListConnector';
import monitorNewItemsOptions from 'Utilities/Artist/monitorNewItemsOptions';
import formatBytes from 'Utilities/Number/formatBytes';

class ArtistEditorRow extends Component {

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
      monitorNewItems,
      metadataProfile,
      qualityProfile,
      path,
      statistics,
      tags,
      columns,
      isSaving,
      isSelected,
      onArtistMonitoredPress,
      onSelectedChange
    } = this.props;

    const monitorNewItemsName = monitorNewItemsOptions.find((o) => o.key === monitorNewItems)?.value;

    return (
      <TableRow>
        <TableSelectCell
          id={id}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'status') {
              return (
                <ArtistStatusCell
                  key={name}
                  artistType={artistType}
                  monitored={monitored}
                  status={status}
                  isSaving={isSaving}
                  onMonitoredPress={onArtistMonitoredPress}
                />
              );
            }

            if (name === 'sortName') {
              return (
                <TableRowCell
                  key={name}
                >
                  <ArtistNameLink
                    foreignArtistId={foreignArtistId}
                    artistName={artistName}
                  />
                </TableRowCell>
              );
            }

            if (name === 'monitorNewItems') {
              return (
                <TableRowCell key={name}>
                  {monitorNewItemsName ?? 'Unknown'}
                </TableRowCell>
              );
            }

            if (name === 'qualityProfileId') {
              return (
                <TableRowCell key={name}>
                  {qualityProfile.name}
                </TableRowCell>
              );
            }

            if (name === 'metadataProfileId') {
              return (
                <TableRowCell key={name}>
                  {metadataProfile.name}
                </TableRowCell>
              );
            }

            if (name === 'path') {
              return (
                <TableRowCell key={name}>
                  {path}
                </TableRowCell>
              );
            }

            if (name === 'sizeOnDisk') {
              return (
                <TableRowCell key={name}>
                  {formatBytes(statistics.sizeOnDisk)}
                </TableRowCell>
              );
            }

            if (name === 'tags') {
              return (
                <TableRowCell key={name}>
                  <TagListConnector
                    tags={tags}
                  />
                </TableRowCell>
              );
            }

            return null;
          })
        }
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
  monitorNewItems: PropTypes.string.isRequired,
  metadataProfile: PropTypes.object.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onArtistMonitoredPress: PropTypes.func.isRequired,
  onSelectedChange: PropTypes.func.isRequired
};

ArtistEditorRow.defaultProps = {
  tags: [],
  statistics: {}
};

export default ArtistEditorRow;
