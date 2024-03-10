import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AlbumFormats from 'Album/AlbumFormats';
import EpisodeStatusConnector from 'Album/EpisodeStatusConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import Tooltip from 'Components/Tooltip/Tooltip';
import { tooltipPositions } from 'Helpers/Props';
import MediaInfoConnector from 'TrackFile/MediaInfoConnector';
import * as mediaInfoTypes from 'TrackFile/mediaInfoTypes';
import formatTimeSpan from 'Utilities/Date/formatTimeSpan';
import formatBytes from 'Utilities/Number/formatBytes';
import formatCustomFormatScore from 'Utilities/Number/formatCustomFormatScore';
import TrackActionsCell from './TrackActionsCell';
import styles from './TrackRow.css';

class TrackRow extends Component {

  //
  // Render

  render() {
    const {
      id,
      albumId,
      mediumNumber,
      trackFileId,
      absoluteTrackNumber,
      title,
      duration,
      isSingleFileRelease,
      trackFilePath,
      trackFileSize,
      customFormats,
      customFormatScore,
      columns,
      deleteTrackFile
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
                    isSingleFileRelease ? `${trackFilePath} (Single File)` : trackFilePath
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

            if (name === 'customFormats') {
              return (
                <TableRowCell key={name}>
                  <AlbumFormats
                    formats={customFormats}
                  />
                </TableRowCell>
              );
            }

            if (name === 'customFormatScore') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.customFormatScore}
                >
                  <Tooltip
                    anchor={formatCustomFormatScore(
                      customFormatScore,
                      customFormats.length
                    )}
                    tooltip={<AlbumFormats formats={customFormats} />}
                    position={tooltipPositions.BOTTOM}
                  />
                </TableRowCell>
              );
            }

            if (name === 'size') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.size}
                >
                  {!!trackFileSize && formatBytes(trackFileSize)}
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

            if (name === 'actions') {
              return (
                <TrackActionsCell
                  key={name}
                  albumId={albumId}
                  id={id}
                  trackFilePath={trackFilePath}
                  trackFileId={trackFileId}
                  deleteTrackFile={deleteTrackFile}
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

TrackRow.propTypes = {
  deleteTrackFile: PropTypes.func.isRequired,
  id: PropTypes.number.isRequired,
  albumId: PropTypes.number.isRequired,
  trackFileId: PropTypes.number,
  mediumNumber: PropTypes.number.isRequired,
  trackNumber: PropTypes.string.isRequired,
  absoluteTrackNumber: PropTypes.number,
  title: PropTypes.string.isRequired,
  duration: PropTypes.number.isRequired,
  isSingleFileRelease: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool,
  trackFilePath: PropTypes.string,
  trackFileSize: PropTypes.number,
  customFormats: PropTypes.arrayOf(PropTypes.object),
  customFormatScore: PropTypes.number.isRequired,
  mediaInfo: PropTypes.object,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired
};

TrackRow.defaultProps = {
  customFormats: []
};

export default TrackRow;
