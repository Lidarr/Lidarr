import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { kinds, sizes } from 'Helpers/Props';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';

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
    this.props.onAlbumSelect(this.props.id);
  }

  //
  // Render

  render() {
    const {
      title,
      albumType,
      releaseDate,
      statistics,
      monitored,
      columns
    } = this.props;

    const {
      trackCount,
      trackFileCount,
      totalTrackCount
    } = statistics;

    return (

      <TableRow>
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'title') {
              return (
                <TableRowCell key={name}>
                  <Link
                    onPress={this.onPress}
                  >
                    {title}
                  </Link>
                </TableRowCell>
              );
            }

            if (name === 'albumType') {
              return (
                <TableRowCell key={name}>
                  {albumType}
                </TableRowCell>
              );
            }

            if (name === 'releaseDate') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  date={releaseDate}
                />
              );
            }

            if (name === 'status') {
              return (
                <TableRowCell
                  key={name}
                >
                  <Label
                    title={`${totalTrackCount} tracks total. ${trackFileCount} tracks with files.`}
                    kind={getTrackCountKind(monitored, trackFileCount, trackCount)}
                    size={sizes.MEDIUM}
                  >
                    {
                      <span>{trackFileCount} / {trackCount}</span>
                    }
                  </Label>
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

SelectAlbumRow.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  albumType: PropTypes.string.isRequired,
  releaseDate: PropTypes.string.isRequired,
  onAlbumSelect: PropTypes.func.isRequired,
  statistics: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired
};

SelectAlbumRow.defaultProps = {
  statistics: {
    trackCount: 0,
    trackFileCount: 0
  }
};

export default SelectAlbumRow;
