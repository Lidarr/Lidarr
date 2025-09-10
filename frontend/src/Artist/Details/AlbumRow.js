import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AlbumSearchCellConnector from 'Album/AlbumSearchCellConnector';
import AlbumTitleLink from 'Album/AlbumTitleLink';
import Label from 'Components/Label';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import StarRating from 'Components/StarRating';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { kinds, sizes } from 'Helpers/Props';
import formatTimeSpan from 'Utilities/Date/formatTimeSpan';
import isAfter from 'Utilities/Date/isAfter';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './AlbumRow.css';

function getTrackCountKind(monitored, releaseDate, trackFileCount, trackCount) {
  if (trackFileCount === trackCount && trackCount > 0) {
    return kinds.SUCCESS;
  }

  if (!releaseDate || isAfter(releaseDate)) {
    return kinds.DISABLED;
  }

  if (!monitored) {
    return kinds.WARNING;
  }

  return kinds.DANGER;
}

class AlbumRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isEditAlbumModalOpen: false
    };
  }

  //
  // Listeners

  onManualSearchPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onEditAlbumPress = () => {
    this.setState({ isEditAlbumModalOpen: true });
  };

  onEditAlbumModalClose = () => {
    this.setState({ isEditAlbumModalOpen: false });
  };

  onMonitorAlbumPress = (monitored, options) => {
    this.props.onMonitorAlbumPress(this.props.id, monitored, options);
  };

  //
  // Render

  render() {
    const {
      id,
      artistId,
      monitored,
      statistics,
      duration,
      releaseDate,
      mediumCount,
      secondaryTypes,
      title,
      ratings,
      disambiguation,
      isSaving,
      artistMonitored,
      foreignAlbumId,
      columns
    } = this.props;

    const {
      trackCount = 0,
      trackFileCount = 0,
      totalTrackCount = 0,
      sizeOnDisk = 0
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

            if (name === 'monitored') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.monitored}
                >
                  <MonitorToggleButton
                    monitored={monitored}
                    isDisabled={!artistMonitored}
                    isSaving={isSaving}
                    onPress={this.onMonitorAlbumPress}
                  />
                </TableRowCell>
              );
            }

            if (name === 'title') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  <AlbumTitleLink
                    foreignAlbumId={foreignAlbumId}
                    title={title}
                    disambiguation={disambiguation}
                  />
                </TableRowCell>
              );
            }

            if (name === 'mediumCount') {
              return (
                <TableRowCell key={name}>
                  {
                    mediumCount
                  }
                </TableRowCell>
              );
            }

            if (name === 'secondaryTypes') {
              return (
                <TableRowCell key={name}>
                  {secondaryTypes.join(', ')}
                </TableRowCell>
              );
            }

            if (name === 'trackCount') {
              return (
                <TableRowCell key={name}>
                  {
                    totalTrackCount
                  }
                </TableRowCell>
              );
            }

            if (name === 'duration') {
              return (
                <TableRowCell key={name}>
                  {
                    formatTimeSpan(duration)
                  }
                </TableRowCell>
              );
            }

            if (name === 'rating') {
              return (
                <TableRowCell key={name}>
                  {
                    <StarRating
                      rating={ratings.value}
                      votes={ratings.votes}
                    />
                  }
                </TableRowCell>
              );
            }

            if (name === 'releaseDate') {
              if ( releaseDate && releaseDate !== '0001-01-01T00:00:00Z' ) {
                return (
                  <RelativeDateCellConnector
                    key={name}
                    date={releaseDate}
                  />
                );
              }
              return (
                <TableRowCell key={name}>
                  { // This is probably a temporary thing. When the metadata server is in full health, this should not happen.
                    // We can either delete it or add a proper translation if we decide to keep it.
                    releaseDate ?
                      translate('Unknown') :
                      'No metadata'
                  }
                </TableRowCell>
              );

            }

            if (name === 'size') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.size}
                >
                  {!!sizeOnDisk && formatBytes(sizeOnDisk)}
                </TableRowCell>
              );
            }

            if (name === 'status') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.status}
                >
                  <Label
                    title={translate('TotalTrackCountTracksTotalTrackFileCountTracksWithFilesInterp', [totalTrackCount, trackFileCount])}
                    kind={getTrackCountKind(monitored, releaseDate, trackFileCount, trackCount)}
                    size={sizes.MEDIUM}
                  >
                    {
                      <span>{trackFileCount} / {trackCount}</span>
                    }
                  </Label>
                </TableRowCell>
              );
            }

            if (name === 'actions') {
              return (
                <AlbumSearchCellConnector
                  key={name}
                  albumId={id}
                  artistId={artistId}
                  albumTitle={title}
                />
              );
            }
            return null;
          })
        }
      </TableRow>
    );
  }
}

AlbumRow.propTypes = {
  id: PropTypes.number.isRequired,
  artistId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  releaseDate: PropTypes.string.isRequired,
  mediumCount: PropTypes.number.isRequired,
  duration: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  disambiguation: PropTypes.string,
  secondaryTypes: PropTypes.arrayOf(PropTypes.string).isRequired,
  foreignAlbumId: PropTypes.string.isRequired,
  isSaving: PropTypes.bool,
  unverifiedSceneNumbering: PropTypes.bool,
  artistMonitored: PropTypes.bool.isRequired,
  statistics: PropTypes.object.isRequired,
  mediaInfo: PropTypes.object,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onMonitorAlbumPress: PropTypes.func.isRequired
};

AlbumRow.defaultProps = {
  statistics: {
    trackCount: 0,
    trackFileCount: 0,
    totalTrackCount: 0
  }
};

export default AlbumRow;
