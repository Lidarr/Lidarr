import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Label from 'Components/Label';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRowButton from 'Components/Table/TableRowButton';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './SelectAlbumRow.css';

function getTrackCountKind(monitored, trackFileCount, trackCount) {
  if (trackFileCount === trackCount && trackCount > 0) {
    return kinds.SUCCESS;
  }

  if (!monitored) {
    return kinds.WARNING;
  }

  return kinds.DANGER;
}

class SelectAlbumRow extends Component {

  //
  // Listeners

  onPress = () => {
    const {
      id,
      isSelected
    } = this.props;

    this.props.onSelectedChange({ id, value: !isSelected });
  };

  //
  // Render

  render() {
    const {
      id,
      foreignAlbumId,
      title,
      disambiguation,
      albumType,
      releaseDate,
      statistics = {},
      monitored,
      isSelected,
      onSelectedChange
    } = this.props;

    const {
      trackCount = 0,
      trackFileCount = 0,
      totalTrackCount = 0
    } = statistics;

    const extendedTitle = disambiguation ? `${title} (${disambiguation})` : title;

    return (
      <TableRowButton onPress={this.onPress}>
        <TableSelectCell
          id={id}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        <TableRowCell>
          {extendedTitle}
        </TableRowCell>

        <TableRowCell>
          {albumType}
        </TableRowCell>

        <RelativeDateCellConnector date={releaseDate} />

        <TableRowCell>
          <Label
            title={translate('TotalTrackCountTracksTotalTrackFileCountTracksWithFilesInterp', { totalTrackCount, trackFileCount })}
            kind={getTrackCountKind(monitored, trackFileCount, trackCount)}
            size={sizes.MEDIUM}
          >
            <span>{trackFileCount} / {trackCount}</span>
          </Label>
        </TableRowCell>

        <TableRowCell className={styles.foreignAlbumId}>
          <Label>{foreignAlbumId}</Label>
        </TableRowCell>
      </TableRowButton>
    );
  }
}

SelectAlbumRow.propTypes = {
  id: PropTypes.number.isRequired,
  foreignAlbumId: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  disambiguation: PropTypes.string.isRequired,
  albumType: PropTypes.string.isRequired,
  releaseDate: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

export default SelectAlbumRow;
