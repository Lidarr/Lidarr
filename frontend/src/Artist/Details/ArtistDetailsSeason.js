import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import Popover from 'Components/Tooltip/Popover';
import { icons, kinds, sizes, sortDirections, tooltipPositions } from 'Helpers/Props';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
import TrackFileEditorModal from 'TrackFile/Editor/TrackFileEditorModal';
import isBefore from 'Utilities/Date/isBefore';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import getToggledRange from 'Utilities/Table/getToggledRange';
import AlbumGroupInfo from './AlbumGroupInfo';
import AlbumRowConnector from './AlbumRowConnector';
import styles from './ArtistDetailsSeason.css';

function getAlbumStatistics(albums) {
  let albumCount = 0;
  let albumFileCount = 0;
  let trackFileCount = 0;
  let totalAlbumCount = 0;
  let monitoredAlbumCount = 0;
  let hasMonitoredAlbums = false;
  let sizeOnDisk = 0;

  albums.forEach(({ monitored, releaseDate, statistics = {} }) => {
    const {
      trackFileCount: albumTrackFileCount = 0,
      totalTrackCount: albumTotalTrackCount = 0,
      sizeOnDisk: albumSizeOnDisk = 0
    } = statistics;

    const hasFiles = albumTrackFileCount > 0 && albumTrackFileCount === albumTotalTrackCount;

    if (hasFiles || (monitored && isBefore(releaseDate))) {
      albumCount++;
    }

    if (hasFiles) {
      albumFileCount++;
    }

    if (monitored) {
      monitoredAlbumCount++;
      hasMonitoredAlbums = true;
    }

    totalAlbumCount++;
    trackFileCount = trackFileCount + albumTrackFileCount;
    sizeOnDisk = sizeOnDisk + albumSizeOnDisk;
  });

  return {
    albumCount,
    albumFileCount,
    totalAlbumCount,
    trackFileCount,
    monitoredAlbumCount,
    sizeOnDisk,
    hasMonitoredAlbums
  };
}

function getAlbumCountKind(monitored, albumCount, albumFileCount) {
  if (albumCount === albumFileCount && albumFileCount > 0) {
    return kinds.SUCCESS;
  }

  if (!monitored) {
    return kinds.WARNING;
  }

  return kinds.DANGER;
}

class ArtistDetailsSeason extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isManageTracksOpen: false,
      lastToggledAlbum: null
    };
  }

  componentDidMount() {
    this._expandByDefault();
  }

  componentDidUpdate(prevProps) {
    const {
      artistId
    } = this.props;

    if (prevProps.artistId !== artistId) {
      this._expandByDefault();
      return;
    }
  }

  //
  // Control

  _expandByDefault() {
    const {
      name,
      onExpandPress,
      items,
      uiSettings
    } = this.props;

    const expand = _.some(items, (item) =>
      ((item.albumType === 'Album') && uiSettings.expandAlbumByDefault) ||
        ((item.albumType === 'Single') && uiSettings.expandSingleByDefault) ||
        ((item.albumType === 'EP') && uiSettings.expandEPByDefault) ||
        ((item.albumType === 'Broadcast') && uiSettings.expandBroadcastByDefault) ||
        ((item.albumType === 'Other') && uiSettings.expandOtherByDefault));

    onExpandPress(name, expand);
  }

  //
  // Listeners

  onOrganizePress = () => {
    this.setState({ isOrganizeModalOpen: true });
  };

  onOrganizeModalClose = () => {
    this.setState({ isOrganizeModalOpen: false });
  };

  onManageTracksPress = () => {
    this.setState({ isManageTracksOpen: true });
  };

  onManageTracksModalClose = () => {
    this.setState({ isManageTracksOpen: false });
  };

  onExpandPress = () => {
    const {
      name,
      isExpanded
    } = this.props;

    this.props.onExpandPress(name, !isExpanded);
  };

  onMonitorAlbumPress = (albumId, monitored, { shiftKey }) => {
    const lastToggled = this.state.lastToggledAlbum;
    const albumIds = [albumId];

    if (shiftKey && lastToggled) {
      const { lower, upper } = getToggledRange(this.props.items, albumId, lastToggled);
      const items = this.props.items;

      for (let i = lower; i < upper; i++) {
        albumIds.push(items[i].id);
      }
    }

    this.setState({ lastToggledAlbum: albumId });

    this.props.onMonitorAlbumsPress(_.uniq(albumIds), monitored);
  };

  onMonitorAlbumsPress = (monitored, { shiftKey }) => {
    const albumIds = this.props.items.map((a) => a.id);

    this.props.onMonitorAlbumsPress(_.uniq(albumIds), monitored);
  };

  //
  // Render

  render() {
    const {
      artistId,
      label,
      items,
      columns,
      isSaving,
      isExpanded,
      artistMonitored,
      sortKey,
      sortDirection,
      onSortPress,
      isSmallScreen,
      onTableOptionChange
    } = this.props;

    const {
      albumCount,
      albumFileCount,
      totalAlbumCount,
      trackFileCount,
      monitoredAlbumCount,
      hasMonitoredAlbums,
      sizeOnDisk = 0
    } = getAlbumStatistics(items);

    const {
      isOrganizeModalOpen,
      isManageTracksOpen
    } = this.state;

    return (
      <div
        className={styles.albumType}
      >
        <div className={styles.header}>
          <div className={styles.left}>
            <MonitorToggleButton
              monitored={hasMonitoredAlbums}
              isDisabled={!artistMonitored}
              isSaving={isSaving}
              size={24}
              onPress={this.onMonitorAlbumsPress}
            />
            <span className={styles.albumTypeLabel}>
              {label}
            </span>
            <Popover
              className={styles.albumCountTooltip}
              canFlip={true}
              anchor={
                <Label
                  size={sizes.LARGE}
                  kind={getAlbumCountKind(hasMonitoredAlbums, albumCount, albumFileCount)}
                >
                  <span>{albumFileCount} / {albumCount}</span>
                </Label>
              }
              title={translate('GroupInformation')}
              body={
                <div>
                  <AlbumGroupInfo
                    totalAlbumCount={totalAlbumCount}
                    monitoredAlbumCount={monitoredAlbumCount}
                    albumFileCount={albumFileCount}
                    trackFileCount={trackFileCount}
                    sizeOnDisk={sizeOnDisk}
                  />
                </div>
              }
              position={tooltipPositions.BOTTOM}
            />

            {
              sizeOnDisk ?
                <div className={styles.sizeOnDisk}>
                  {formatBytes(sizeOnDisk)}
                </div> :
                null
            }
          </div>

          <Link
            className={styles.expandButton}
            onPress={this.onExpandPress}
          >

            <Icon
              className={styles.expandButtonIcon}
              name={isExpanded ? icons.COLLAPSE : icons.EXPAND}
              title={isExpanded ? translate('IsExpandedHideAlbums') : translate('IsExpandedShowAlbums')}
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
              <div className={styles.albums}>
                {
                  items.length ?
                    <Table
                      columns={columns}
                      sortKey={sortKey}
                      sortDirection={sortDirection}
                      onSortPress={onSortPress}
                      onTableOptionChange={onTableOptionChange}
                    >
                      <TableBody>
                        {
                          items.map((item) => {
                            return (
                              <AlbumRowConnector
                                key={item.id}
                                columns={columns}
                                {...item}
                                onMonitorAlbumPress={this.onMonitorAlbumPress}
                              />
                            );
                          })
                        }
                      </TableBody>
                    </Table> :

                    <div className={styles.noAlbums}>
                      No releases in this group
                    </div>
                }
                <div className={styles.collapseButtonContainer}>
                  <IconButton
                    iconClassName={styles.collapseButtonIcon}
                    name={icons.COLLAPSE}
                    size={20}
                    title={translate('HideAlbums')}
                    onPress={this.onExpandPress}
                  />
                </div>
              </div>
          }
        </div>

        <OrganizePreviewModalConnector
          isOpen={isOrganizeModalOpen}
          artistId={artistId}
          onModalClose={this.onOrganizeModalClose}
        />

        <TrackFileEditorModal
          isOpen={isManageTracksOpen}
          artistId={artistId}
          onModalClose={this.onManageTracksModalClose}
        />
      </div>
    );
  }
}

ArtistDetailsSeason.propTypes = {
  artistId: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  label: PropTypes.string.isRequired,
  artistMonitored: PropTypes.bool.isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool,
  isExpanded: PropTypes.bool,
  isSmallScreen: PropTypes.bool.isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  onExpandPress: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  onMonitorAlbumsPress: PropTypes.func.isRequired,
  uiSettings: PropTypes.object.isRequired
};

export default ArtistDetailsSeason;
