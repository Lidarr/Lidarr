import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AlbumFormats from 'Album/AlbumFormats';
import AlbumTitleLink from 'Album/AlbumTitleLink';
import EpisodeStatusConnector from 'Album/EpisodeStatusConnector';
import IndexerFlags from 'Album/IndexerFlags';
import AlbumInteractiveSearchModalConnector from 'Album/Search/AlbumInteractiveSearchModalConnector';
import * as commandNames from 'Commands/commandNames';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import MediaInfoConnector from 'TrackFile/MediaInfoConnector';
import * as mediaInfoTypes from 'TrackFile/mediaInfoTypes';
import formatTimeSpan from 'Utilities/Date/formatTimeSpan';
import formatBytes from 'Utilities/Number/formatBytes';
import formatCustomFormatScore from 'Utilities/Number/formatCustomFormatScore';
import translate from 'Utilities/String/translate';
import TrackActionsCell from './TrackActionsCell';
import styles from './TrackRow.css';

class TrackRow extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      isInteractiveSearchModalOpen: false
    };
  }

  onMonitorTogglePress = (monitored) => {
    this.props.toggleTracksMonitored({
      trackIds: this.props.monitorTrackIds || [this.props.id],
      monitored
    });
  };

  onSearchPress = () => {
    this.props.executeCommand({
      name: commandNames.TRACK_SEARCH,
      trackIds: [this.props.id]
    });
  };

  onInteractiveSearchPress = () => {
    this.setState({ isInteractiveSearchModalOpen: true });
  };

  onInteractiveSearchModalClose = () => {
    this.setState({ isInteractiveSearchModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      id,
      albumId,
      albumTitle,
      foreignAlbumId,
      mediumNumber,
      trackFileId,
      absoluteTrackNumber,
      title,
      duration,
      trackFilePath,
      trackFileSize,
      customFormats,
      customFormatScore,
      indexerFlags,
      monitored,
      appearanceCount,
      isSaving,
      isSearching,
      isSelectMode,
      isSelected,
      selectionId,
      columns,
      deleteTrackFile,
      onSelectedChange
    } = this.props;

    return (
      <TableRow>
        {
          isSelectMode ?
            <TableSelectCell
              id={selectionId || id}
              isSelected={isSelected}
              onSelectedChange={onSelectedChange}
            /> :
            null
        }

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

            if (name === 'monitored') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.monitored}
                >
                  <MonitorToggleButton
                    monitored={monitored}
                    isSaving={isSaving}
                    size={14}
                    onPress={this.onMonitorTogglePress}
                  />
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

            if (name === 'album') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.album}
                >
                  {
                    foreignAlbumId ?
                      <AlbumTitleLink
                        foreignAlbumId={foreignAlbumId}
                        title={albumTitle}
                      /> :
                      albumTitle
                  }
                </TableRowCell>
              );
            }

            if (name === 'popularity') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.popularity}
                  title={translate('ReleaseAppearancesCountInterp', [appearanceCount])}
                >
                  {appearanceCount}
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
                    position={tooltipPositions.LEFT}
                  />
                </TableRowCell>
              );
            }

            if (name === 'indexerFlags') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexerFlags}
                >
                  {indexerFlags ? (
                    <Popover
                      anchor={<Icon name={icons.FLAG} kind={kinds.PRIMARY} />}
                      title={translate('IndexerFlags')}
                      body={<IndexerFlags indexerFlags={indexerFlags} />}
                      position={tooltipPositions.LEFT}
                    />
                  ) : null}
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
                    albumId={albumId}
                    trackFileId={trackFileId}
                  />
                </TableRowCell>
              );
            }

            if (name === 'search') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.search}
                >
                  <SpinnerIconButton
                    name={icons.SEARCH}
                    isSpinning={isSearching}
                    title={translate('AutomaticSearch')}
                    onPress={this.onSearchPress}
                  />

                  <IconButton
                    name={icons.INTERACTIVE}
                    title={translate('InteractiveSearch')}
                    onPress={this.onInteractiveSearchPress}
                  />

                  <AlbumInteractiveSearchModalConnector
                    isOpen={this.state.isInteractiveSearchModalOpen}
                    albumId={albumId}
                    albumTitle={albumTitle}
                    trackId={id}
                    onModalClose={this.onInteractiveSearchModalClose}
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
  albumTitle: PropTypes.string.isRequired,
  foreignAlbumId: PropTypes.string,
  monitorTrackIds: PropTypes.arrayOf(PropTypes.number),
  appearanceCount: PropTypes.number,
  trackFileId: PropTypes.number,
  mediumNumber: PropTypes.number.isRequired,
  trackNumber: PropTypes.string.isRequired,
  absoluteTrackNumber: PropTypes.number,
  title: PropTypes.string.isRequired,
  duration: PropTypes.number.isRequired,
  isSaving: PropTypes.bool,
  isSearching: PropTypes.bool.isRequired,
  isSelectMode: PropTypes.bool,
  isSelected: PropTypes.bool,
  selectionId: PropTypes.string,
  monitored: PropTypes.bool.isRequired,
  trackFilePath: PropTypes.string,
  trackFileSize: PropTypes.number,
  customFormats: PropTypes.arrayOf(PropTypes.object),
  customFormatScore: PropTypes.number.isRequired,
  indexerFlags: PropTypes.number.isRequired,
  mediaInfo: PropTypes.object,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  toggleTracksMonitored: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired,
  onSelectedChange: PropTypes.func
};

TrackRow.defaultProps = {
  appearanceCount: 1,
  customFormats: [],
  indexerFlags: 0,
  isSelectMode: false,
  isSelected: false
};

export default TrackRow;
