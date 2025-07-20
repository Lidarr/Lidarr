import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds, sizes } from 'Helpers/Props';
import isAfter from 'Utilities/Date/isAfter';
import translate from 'Utilities/String/translate';
import TrackRowConnector from './TrackRowConnector';
import styles from './AlbumDetailsMedium.css';

function getMediumStatistics(tracks) {
  const trackCount = tracks.length;
  let trackFileCount = 0;
  let totalTrackCount = 0;

  tracks.forEach((track) => {
    if (track.trackFileId) {
      trackFileCount++;
    }

    totalTrackCount++;
  });

  return {
    trackCount,
    trackFileCount,
    totalTrackCount
  };
}

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

class AlbumDetailsMedium extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this._expandByDefault();
  }

  componentDidUpdate(prevProps) {
    if (prevProps.albumId !== this.props.albumId) {
      this._expandByDefault();
    }
  }

  //
  // Control

  _expandByDefault() {
    const {
      mediumNumber,
      onExpandPress
    } = this.props;

    onExpandPress(mediumNumber, mediumNumber === 1);
  }

  //
  // Listeners

  onExpandPress = () => {
    const {
      mediumNumber,
      isExpanded
    } = this.props;

    this.props.onExpandPress(mediumNumber, !isExpanded);
  };

  //
  // Render

  render() {
    const {
      mediumNumber,
      mediumFormat,
      albumMonitored,
      albumReleaseDate,
      items,
      columns,
      onTableOptionChange,
      isExpanded,
      isSmallScreen
    } = this.props;

    const {
      trackCount,
      trackFileCount,
      totalTrackCount
    } = getMediumStatistics(items);

    return (
      <div
        className={styles.medium}
      >
        <div className={styles.header}>
          <div className={styles.left}>
            {
              <div>
                <span className={styles.mediumNumber}>
                  {mediumFormat} {mediumNumber}
                </span>
              </div>
            }

            <Label
              title={translate('TotalTrackCountTracksTotalTrackFileCountTracksWithFilesInterp', [totalTrackCount, trackFileCount])}
              kind={getTrackCountKind(albumMonitored, albumReleaseDate, trackFileCount, trackCount)}
              size={sizes.LARGE}
            >
              {
                <span>{trackFileCount} / {trackCount}</span>
              }
            </Label>
          </div>

          <Link
            className={styles.expandButton}
            onPress={this.onExpandPress}
          >
            <Icon
              className={styles.expandButtonIcon}
              name={isExpanded ? icons.COLLAPSE : icons.EXPAND}
              title={isExpanded ? translate('IsExpandedHideTracks') : translate('IsExpandedShowTracks')}
              size={24}
            />
            {
              !isSmallScreen &&
                <span>&nbsp;</span>
            }
          </Link>

        </div>

        <div>
          {
            isExpanded &&
              <div className={styles.tracks}>
                {
                  items.length ?
                    <Table
                      columns={columns}
                      onTableOptionChange={onTableOptionChange}
                    >
                      <TableBody>
                        {
                          items.map((item) => {
                            return (
                              <TrackRowConnector
                                key={item.id}
                                columns={columns}
                                {...item}
                              />
                            );
                          })
                        }
                      </TableBody>
                    </Table> :

                    <div className={styles.noTracks}>
                      {translate('NoTracksInThisMedium')}
                    </div>
                }
                <div className={styles.collapseButtonContainer}>
                  <IconButton
                    name={icons.COLLAPSE}
                    size={20}
                    title={translate('HideTracks')}
                    onPress={this.onExpandPress}
                  />
                </div>
              </div>
          }
        </div>
      </div>
    );
  }
}

AlbumDetailsMedium.propTypes = {
  albumId: PropTypes.number.isRequired,
  albumMonitored: PropTypes.bool.isRequired,
  albumReleaseDate: PropTypes.string,
  mediumNumber: PropTypes.number.isRequired,
  mediumFormat: PropTypes.string.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool,
  isExpanded: PropTypes.bool,
  isSmallScreen: PropTypes.bool.isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  onExpandPress: PropTypes.func.isRequired
};

export default AlbumDetailsMedium;
