import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import formatTimeSpan from 'Utilities/Date/formatTimeSpan';
import EpisodeStatusConnector from 'Album/EpisodeStatusConnector';
import TrackFileLanguageConnector from 'TrackFile/TrackFileLanguageConnector';
import MediaInfoConnector from 'TrackFile/MediaInfoConnector';
import * as mediaInfoTypes from 'TrackFile/mediaInfoTypes';

import styles from './TrackRow.css';

class TrackRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false
    };
  }

  //
  // Listeners

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  }

  //
  // Render

  render() {
    const {
      id,
      mediumNumber,
      trackFileId,
      absoluteTrackNumber,
      title,
      duration,
      trackFilePath,
      trackFileRelativePath,
      columns
    } = this.props;

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

            if (name === 'medium') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.trackNumber}
                >
                  {mediumNumber}
                </TableRowCell>
              );
            }

            if (name === 'absoluteTrackNumber') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.trackNumber}
                >
                  {absoluteTrackNumber}
                </TableRowCell>
              );
            }

            if (name === 'title') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  {title}
                </TableRowCell>
              );
            }

            if (name === 'path') {
              return (
                <TableRowCell key={name}>
                  {
                    trackFilePath
                  }
                </TableRowCell>
              );
            }

            if (name === 'relativePath') {
              return (
                <TableRowCell key={name}>
                  {
                    trackFileRelativePath
                  }
                </TableRowCell>
              );
            }

            if (name === 'duration') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.duration}
                >
                  {
                    formatTimeSpan(duration)
                  }
                </TableRowCell>
              );
            }

            if (name === 'language') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.language}
                >
                  <TrackFileLanguageConnector
                    episodeFileId={trackFileId}
                  />
                </TableRowCell>
              );
            }

            if (name === 'audioInfo') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.audio}
                >
                  <MediaInfoConnector
                    type={mediaInfoTypes.AUDIO}
                    trackFileId={trackFileId}
                  />
                </TableRowCell>
              );
            }

            if (name === 'status') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.status}
                >
                  <EpisodeStatusConnector
                    albumId={id}
                    trackFileId={trackFileId}
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

TrackRow.propTypes = {
  id: PropTypes.number.isRequired,
  albumId: PropTypes.number.isRequired,
  trackFileId: PropTypes.number,
  mediumNumber: PropTypes.number.isRequired,
  trackNumber: PropTypes.string.isRequired,
  absoluteTrackNumber: PropTypes.number,
  title: PropTypes.string.isRequired,
  duration: PropTypes.number.isRequired,
  isSaving: PropTypes.bool,
  trackFilePath: PropTypes.string,
  trackFileRelativePath: PropTypes.string,
  mediaInfo: PropTypes.object,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default TrackRow;
