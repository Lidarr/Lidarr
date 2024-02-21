import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AlbumFormats from 'Album/AlbumFormats';
import IndexerFlags from 'Album/IndexerFlags';
import TrackQuality from 'Album/TrackQuality';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRowCellButton from 'Components/Table/Cells/TableRowCellButton';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import { icons, kinds, sortDirections, tooltipPositions } from 'Helpers/Props';
import SelectAlbumModal from 'InteractiveImport/Album/SelectAlbumModal';
import SelectArtistModal from 'InteractiveImport/Artist/SelectArtistModal';
import SelectIndexerFlagsModal from 'InteractiveImport/IndexerFlags/SelectIndexerFlagsModal';
import SelectQualityModal from 'InteractiveImport/Quality/SelectQualityModal';
import SelectReleaseGroupModal from 'InteractiveImport/ReleaseGroup/SelectReleaseGroupModal';
import SelectTrackModal from 'InteractiveImport/Track/SelectTrackModal';
import formatBytes from 'Utilities/Number/formatBytes';
import hasDifferentItems from 'Utilities/Object/hasDifferentItems';
import translate from 'Utilities/String/translate';
import InteractiveImportRowCellPlaceholder from './InteractiveImportRowCellPlaceholder';
import styles from './InteractiveImportRow.css';

class InteractiveImportRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isSelectArtistModalOpen: false,
      isSelectAlbumModalOpen: false,
      isSelectTrackModalOpen: false,
      isSelectReleaseGroupModalOpen: false,
      isSelectQualityModalOpen: false,
      isSelectIndexerFlagsModalOpen: false
    };
  }

  componentDidMount() {
    const {
      id,
      artist,
      album,
      tracks,
      quality
    } = this.props;

    if (
      artist &&
      album != null &&
      tracks.length &&
      quality
    ) {
      this.props.onSelectedChange({ id, value: true });
    }
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      artist,
      album,
      tracks,
      quality,
      isSelected,
      onValidRowChange
    } = this.props;

    if (
      prevProps.artist === artist &&
      prevProps.album === album &&
      !hasDifferentItems(prevProps.tracks, tracks) &&
      prevProps.quality === quality &&
      prevProps.isSelected === isSelected
    ) {
      return;
    }

    const isValid = !!(
      artist &&
      album &&
      tracks.length &&
      quality
    );

    if (isSelected && !isValid) {
      onValidRowChange(id, false);
    } else {
      onValidRowChange(id, true);
    }
  }

  //
  // Control

  selectRowAfterChange = (value) => {
    const {
      id,
      isSelected
    } = this.props;

    if (!isSelected && value === true) {
      this.props.onSelectedChange({ id, value });
    }
  };

  //
  // Listeners

  onSelectArtistPress = () => {
    this.setState({ isSelectArtistModalOpen: true });
  };

  onSelectAlbumPress = () => {
    this.setState({ isSelectAlbumModalOpen: true });
  };

  onSelectTrackPress = () => {
    this.setState({ isSelectTrackModalOpen: true });
  };

  onSelectReleaseGroupPress = () => {
    this.setState({ isSelectReleaseGroupModalOpen: true });
  };

  onSelectQualityPress = () => {
    this.setState({ isSelectQualityModalOpen: true });
  };

  onSelectIndexerFlagsPress = () => {
    this.setState({ isSelectIndexerFlagsModalOpen: true });
  };

  onSelectArtistModalClose = (changed) => {
    this.setState({ isSelectArtistModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectAlbumModalClose = (changed) => {
    this.setState({ isSelectAlbumModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectTrackModalClose = (changed) => {
    this.setState({ isSelectTrackModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectReleaseGroupModalClose = (changed) => {
    this.setState({ isSelectReleaseGroupModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectQualityModalClose = (changed) => {
    this.setState({ isSelectQualityModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectIndexerFlagsModalClose = (changed) => {
    this.setState({ isSelectIndexerFlagsModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  //
  // Render

  render() {
    const {
      id,
      allowArtistChange,
      path,
      artist,
      album,
      albumReleaseId,
      tracks,
      quality,
      releaseGroup,
      size,
      customFormats,
      indexerFlags,
      rejections,
      columns,
      isReprocessing,
      audioTags,
      additionalFile,
      isSelected,
      onSelectedChange
    } = this.props;

    const {
      isSelectArtistModalOpen,
      isSelectAlbumModalOpen,
      isSelectTrackModalOpen,
      isSelectReleaseGroupModalOpen,
      isSelectQualityModalOpen,
      isSelectIndexerFlagsModalOpen
    } = this.state;

    const artistName = artist ? artist.artistName : '';
    let albumTitle = '';
    if (album) {
      albumTitle = album.disambiguation ? `${album.title} (${album.disambiguation})` : album.title;
    }

    const sortedTracks = tracks.sort((a, b) => parseInt(a.absoluteTrackNumber) - parseInt(b.absoluteTrackNumber));

    const trackNumbers = sortedTracks.map((track) => `${track.mediumNumber}x${track.trackNumber}`)
      .join(', ');

    const showArtistPlaceholder = isSelected && !artist;
    const showAlbumNumberPlaceholder = isSelected && !!artist && !album;
    const showTrackNumbersPlaceholder = !isReprocessing && isSelected && !!album && !tracks.length;
    const showTrackNumbersLoading = isReprocessing && !tracks.length;
    const showReleaseGroupPlaceholder = isSelected && !releaseGroup;
    const showQualityPlaceholder = isSelected && !quality;
    const showIndexerFlagsPlaceholder = isSelected && !indexerFlags;

    const pathCellContents = (
      <div>
        {path}
      </div>
    );

    const pathCell = additionalFile ? (
      <Tooltip
        anchor={pathCellContents}
        tooltip={translate('AnchorTooltip')}
        position={tooltipPositions.TOP}
      />
    ) : pathCellContents;

    const isIndexerFlagsColumnVisible = columns.find((c) => c.name === 'indexerFlags')?.isVisible ?? false;

    return (
      <TableRow
        className={additionalFile ? styles.additionalFile : undefined}
      >
        <TableSelectCell
          id={id}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        <TableRowCell
          className={styles.path}
          title={path}
        >
          {pathCell}
        </TableRowCell>

        <TableRowCellButton
          isDisabled={!allowArtistChange}
          title={allowArtistChange ? translate('AllowArtistChangeClickToChangeArtist') : undefined}
          onPress={this.onSelectArtistPress}
        >
          {
            showArtistPlaceholder ? <InteractiveImportRowCellPlaceholder /> : artistName
          }
        </TableRowCellButton>

        <TableRowCellButton
          isDisabled={!artist}
          title={artist ? translate('ArtistClickToChangeAlbum') : undefined}
          onPress={this.onSelectAlbumPress}
        >
          {
            showAlbumNumberPlaceholder ? <InteractiveImportRowCellPlaceholder /> : albumTitle
          }
        </TableRowCellButton>

        <TableRowCellButton
          isDisabled={!artist || !album}
          title={artist && album ? translate('ArtistAlbumClickToChangeTrack') : undefined}
          onPress={this.onSelectTrackPress}
        >
          {
            showTrackNumbersLoading && <LoadingIndicator size={20} className={styles.loading} />
          }
          {
            showTrackNumbersPlaceholder ? <InteractiveImportRowCellPlaceholder /> : trackNumbers
          }
        </TableRowCellButton>

        <TableRowCellButton
          title={translate('ClickToChangeReleaseGroup')}
          onPress={this.onSelectReleaseGroupPress}
        >
          {
            showReleaseGroupPlaceholder ?
              <InteractiveImportRowCellPlaceholder isOptional={true} /> :
              releaseGroup
          }
        </TableRowCellButton>

        <TableRowCellButton
          className={styles.quality}
          title={translate('ClickToChangeQuality')}
          onPress={this.onSelectQualityPress}
        >
          {
            showQualityPlaceholder &&
              <InteractiveImportRowCellPlaceholder />
          }

          {
            !showQualityPlaceholder && !!quality &&
              <TrackQuality
                className={styles.label}
                quality={quality}
              />
          }
        </TableRowCellButton>

        <TableRowCell>
          {formatBytes(size)}
        </TableRowCell>

        <TableRowCell>
          {
            customFormats?.length ?
              <Popover
                anchor={
                  <Icon name={icons.INTERACTIVE} />
                }
                title={translate('Formats')}
                body={
                  <div className={styles.customFormatTooltip}>
                    <AlbumFormats formats={customFormats} />
                  </div>
                }
                position={tooltipPositions.LEFT}
              /> :
              null
          }
        </TableRowCell>

        {isIndexerFlagsColumnVisible ? (
          <TableRowCellButton
            title={translate('ClickToChangeIndexerFlags')}
            onPress={this.onSelectIndexerFlagsPress}
          >
            {showIndexerFlagsPlaceholder ? (
              <InteractiveImportRowCellPlaceholder isOptional={true} />
            ) : (
              <>
                {indexerFlags ? (
                  <Popover
                    anchor={<Icon name={icons.FLAG} kind={kinds.PRIMARY} />}
                    title={translate('IndexerFlags')}
                    body={<IndexerFlags indexerFlags={indexerFlags} />}
                    position={tooltipPositions.LEFT}
                  />
                ) : null}
              </>
            )}
          </TableRowCellButton>
        ) : null}

        <TableRowCell>
          {
            rejections.length ?
              <Popover
                anchor={
                  <Icon
                    name={icons.DANGER}
                    kind={kinds.DANGER}
                  />
                }
                title={translate('ReleaseRejected')}
                body={
                  <ul>
                    {
                      rejections.map((rejection, index) => {
                        return (
                          <li key={index}>
                            {rejection.reason}
                          </li>
                        );
                      })
                    }
                  </ul>
                }
                position={tooltipPositions.LEFT}
                canFlip={false}
              /> :
              null
          }
        </TableRowCell>

        <SelectArtistModal
          isOpen={isSelectArtistModalOpen}
          ids={[id]}
          onModalClose={this.onSelectArtistModalClose}
        />

        <SelectAlbumModal
          isOpen={isSelectAlbumModalOpen}
          ids={[id]}
          artistId={artist && artist.id}
          onModalClose={this.onSelectAlbumModalClose}
        />

        <SelectTrackModal
          isOpen={isSelectTrackModalOpen}
          id={id}
          artistId={artist && artist.id}
          albumId={album && album.id}
          albumReleaseId={albumReleaseId}
          rejections={rejections}
          audioTags={audioTags}
          sortKey='mediumNumber'
          sortDirection={sortDirections.ASCENDING}
          filename={path}
          onModalClose={this.onSelectTrackModalClose}
        />

        <SelectReleaseGroupModal
          isOpen={isSelectReleaseGroupModalOpen}
          ids={[id]}
          releaseGroup={releaseGroup ?? ''}
          onModalClose={this.onSelectReleaseGroupModalClose}
        />

        <SelectQualityModal
          isOpen={isSelectQualityModalOpen}
          ids={[id]}
          qualityId={quality ? quality.quality.id : 0}
          proper={quality ? quality.revision.version > 1 : false}
          real={quality ? quality.revision.real > 0 : false}
          onModalClose={this.onSelectQualityModalClose}
        />

        <SelectIndexerFlagsModal
          isOpen={isSelectIndexerFlagsModalOpen}
          ids={[id]}
          indexerFlags={indexerFlags ?? 0}
          onModalClose={this.onSelectIndexerFlagsModalClose}
        />
      </TableRow>
    );
  }

}

InteractiveImportRow.propTypes = {
  id: PropTypes.number.isRequired,
  allowArtistChange: PropTypes.bool.isRequired,
  path: PropTypes.string.isRequired,
  artist: PropTypes.object,
  album: PropTypes.object,
  albumReleaseId: PropTypes.number,
  tracks: PropTypes.arrayOf(PropTypes.object).isRequired,
  releaseGroup: PropTypes.string,
  quality: PropTypes.object,
  size: PropTypes.number.isRequired,
  customFormats: PropTypes.arrayOf(PropTypes.object),
  indexerFlags: PropTypes.number.isRequired,
  rejections: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  audioTags: PropTypes.object.isRequired,
  additionalFile: PropTypes.bool.isRequired,
  isReprocessing: PropTypes.bool,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  onValidRowChange: PropTypes.func.isRequired
};

InteractiveImportRow.defaultProps = {
  tracks: []
};

export default InteractiveImportRow;
