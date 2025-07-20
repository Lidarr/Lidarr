import _ from 'lodash';
import moment from 'moment';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import AlbumCover from 'Album/AlbumCover';
import DeleteAlbumModal from 'Album/Delete/DeleteAlbumModal';
import EditAlbumModalConnector from 'Album/Edit/EditAlbumModalConnector';
import AlbumInteractiveSearchModalConnector from 'Album/Search/AlbumInteractiveSearchModalConnector';
import ArtistGenres from 'Artist/Details/ArtistGenres';
import ArtistHistoryModal from 'Artist/History/ArtistHistoryModal';
import Alert from 'Components/Alert';
import HeartRating from 'Components/HeartRating';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import Tooltip from 'Components/Tooltip/Tooltip';
import { align, icons, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
import RetagPreviewModalConnector from 'Retag/RetagPreviewModalConnector';
import fonts from 'Styles/Variables/fonts';
import TrackFileEditorModal from 'TrackFile/Editor/TrackFileEditorModal';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import AlbumDetailsLinks from './AlbumDetailsLinks';
import AlbumDetailsMediumConnector from './AlbumDetailsMediumConnector';
import styles from './AlbumDetails.css';

const intermediateFontSize = parseInt(fonts.intermediateFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

function getFanartUrl(images) {
  return _.find(images, { coverType: 'fanart' })?.url;
}

function formatDuration(timeSpan) {
  const duration = moment.duration(timeSpan);
  const hours = duration.get('hours');
  const minutes = duration.get('minutes');
  let hoursText = 'Hours';
  let minText = 'Minutes';

  if (minutes === 1) {
    minText = 'Minute';
  }

  if (hours === 0) {
    return `${minutes} ${minText}`;
  }

  if (hours === 1) {
    hoursText = 'Hour';
  }

  return `${hours} ${hoursText} ${minutes} ${minText}`;
}

function getExpandedState(newState) {
  return {
    allExpanded: newState.allSelected,
    allCollapsed: newState.allUnselected,
    expandedState: newState.selectedState
  };
}

class AlbumDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isRetagModalOpen: false,
      isArtistHistoryModalOpen: false,
      isInteractiveSearchModalOpen: false,
      isManageTracksOpen: false,
      isEditAlbumModalOpen: false,
      isDeleteAlbumModalOpen: false,
      allExpanded: false,
      allCollapsed: false,
      expandedState: {}
    };
  }

  //
  // Listeners

  onOrganizePress = () => {
    this.setState({ isOrganizeModalOpen: true });
  };

  onOrganizeModalClose = () => {
    this.setState({ isOrganizeModalOpen: false });
  };

  onRetagPress = () => {
    this.setState({ isRetagModalOpen: true });
  };

  onRetagModalClose = () => {
    this.setState({ isRetagModalOpen: false });
  };

  onEditAlbumPress = () => {
    this.setState({ isEditAlbumModalOpen: true });
  };

  onEditAlbumModalClose = () => {
    this.setState({ isEditAlbumModalOpen: false });
  };

  onDeleteAlbumPress = () => {
    this.setState({
      isEditAlbumModalOpen: false,
      isDeleteAlbumModalOpen: true
    });
  };

  onDeleteAlbumModalClose = () => {
    this.setState({ isDeleteAlbumModalOpen: false });
  };

  onManageTracksPress = () => {
    this.setState({ isManageTracksOpen: true });
  };

  onManageTracksModalClose = () => {
    this.setState({ isManageTracksOpen: false });
  };

  onInteractiveSearchPress = () => {
    this.setState({ isInteractiveSearchModalOpen: true });
  };

  onInteractiveSearchModalClose = () => {
    this.setState({ isInteractiveSearchModalOpen: false });
  };

  onArtistHistoryPress = () => {
    this.setState({ isArtistHistoryModalOpen: true });
  };

  onArtistHistoryModalClose = () => {
    this.setState({ isArtistHistoryModalOpen: false });
  };

  onExpandAllPress = () => {
    const {
      allExpanded,
      expandedState
    } = this.state;

    this.setState(getExpandedState(selectAll(expandedState, !allExpanded)));
  };

  onExpandPress = (albumId, isExpanded) => {
    this.setState((state) => {
      const convertedState = {
        allSelected: state.allExpanded,
        allUnselected: state.allCollapsed,
        selectedState: state.expandedState
      };

      const newState = toggleSelected(convertedState, [], albumId, isExpanded, false);

      return getExpandedState(newState);
    });
  };

  //
  // Render

  render() {
    const {
      id,
      foreignAlbumId,
      title,
      disambiguation,
      duration,
      overview,
      albumType,
      secondaryTypes,
      statistics = {},
      monitored,
      releaseDate,
      ratings,
      images,
      genres,
      links,
      media,
      isSaving,
      isFetching,
      isPopulated,
      albumsError,
      tracksError,
      trackFilesError,
      hasTrackFiles,
      shortDateFormat,
      artist,
      previousAlbum,
      nextAlbum,
      isSearching,
      onMonitorTogglePress,
      onSearchPress
    } = this.props;

    const {
      trackFileCount = 0,
      sizeOnDisk = 0
    } = statistics;

    const {
      isOrganizeModalOpen,
      isRetagModalOpen,
      isArtistHistoryModalOpen,
      isInteractiveSearchModalOpen,
      isEditAlbumModalOpen,
      isDeleteAlbumModalOpen,
      isManageTracksOpen,
      allExpanded,
      allCollapsed,
      expandedState
    } = this.state;

    const fanartUrl = getFanartUrl(artist.images);

    let expandIcon = icons.EXPAND_INDETERMINATE;
    let trackFilesCountMessage = translate('TrackFilesCountMessage');

    if (trackFileCount === 1) {
      trackFilesCountMessage = '1 track file';
    } else if (trackFileCount > 1) {
      trackFilesCountMessage = `${trackFileCount} track files`;
    }

    if (allExpanded) {
      expandIcon = icons.COLLAPSE;
    } else if (allCollapsed) {
      expandIcon = icons.EXPAND;
    }

    return (
      <PageContent title={title}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('SearchAlbum')}
              iconName={icons.SEARCH}
              isSpinning={isSearching}
              onPress={onSearchPress}
            />

            <PageToolbarButton
              label={translate('InteractiveSearch')}
              iconName={icons.INTERACTIVE}
              onPress={this.onInteractiveSearchPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('PreviewRename')}
              iconName={icons.ORGANIZE}
              isDisabled={!hasTrackFiles}
              onPress={this.onOrganizePress}
            />

            <PageToolbarButton
              label={translate('PreviewRetag')}
              iconName={icons.RETAG}
              isDisabled={!hasTrackFiles}
              onPress={this.onRetagPress}
            />

            <PageToolbarButton
              label={translate('ManageTracks')}
              iconName={icons.TRACK_FILE}
              isDisabled={!hasTrackFiles}
              onPress={this.onManageTracksPress}
            />

            <PageToolbarButton
              label={translate('History')}
              iconName={icons.HISTORY}
              onPress={this.onArtistHistoryPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('Edit')}
              iconName={icons.EDIT}
              onPress={this.onEditAlbumPress}
            />

            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              onPress={this.onDeleteAlbumPress}
            />

          </PageToolbarSection>
          <PageToolbarSection alignContent={align.RIGHT}>
            <PageToolbarButton
              label={allExpanded ? translate('AllExpandedCollapseAll') : translate('AllExpandedExpandAll')}
              iconName={expandIcon}
              onPress={this.onExpandAllPress}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody innerClassName={styles.innerContentBody}>
          <div className={styles.header}>
            <div
              className={styles.backdrop}
              style={
                fanartUrl ?
                  { backgroundImage: `url(${fanartUrl})` } :
                  null
              }
            >
              <div className={styles.backdropOverlay} />
            </div>

            <div className={styles.headerContent}>
              <AlbumCover
                className={styles.cover}
                images={images}
                size={250}
                lazy={false}
              />

              <div className={styles.info}>
                <div className={styles.titleRow}>
                  <div className={styles.titleContainer}>

                    <div className={styles.toggleMonitoredContainer}>
                      <MonitorToggleButton
                        className={styles.monitorToggleButton}
                        monitored={monitored}
                        isSaving={isSaving}
                        size={40}
                        onPress={onMonitorTogglePress}
                      />
                    </div>

                    <div
                      className={styles.title}
                      title={disambiguation ? `${title} (${disambiguation})` : title}
                    >
                      <TextTruncate
                        line={2}
                        text={disambiguation ? `${title} (${disambiguation})` : title}
                      />
                    </div>
                  </div>

                  <div className={styles.albumNavigationButtons}>
                    <IconButton
                      className={styles.albumNavigationButton}
                      name={icons.ARROW_LEFT}
                      size={30}
                      title={translate('GoToInterp', [previousAlbum.title])}
                      to={`/album/${previousAlbum.foreignAlbumId}`}
                    />

                    <IconButton
                      className={styles.albumNavigationButton}
                      name={icons.ARROW_UP}
                      size={30}
                      title={translate('GoToInterp', [artist.artistName])}
                      to={`/artist/${artist.foreignArtistId}`}
                    />

                    <IconButton
                      className={styles.albumNavigationButton}
                      name={icons.ARROW_RIGHT}
                      size={30}
                      title={translate('GoToInterp', [nextAlbum.title])}
                      to={`/album/${nextAlbum.foreignAlbumId}`}
                    />
                  </div>
                </div>

                <div className={styles.details}>
                  <div>
                    {
                      duration ?
                        <span className={styles.duration}>
                          {formatDuration(duration)}
                        </span> :
                        null
                    }

                    <HeartRating
                      rating={ratings.value}
                      iconSize={20}
                    />

                    <ArtistGenres genres={genres} />
                  </div>
                </div>

                <div className={styles.detailsLabels}>

                  <Label
                    className={styles.detailsLabel}
                    title={translate('ReleaseDate')}
                    size={sizes.LARGE}
                  >
                    <div>
                      <Icon
                        name={icons.CALENDAR}
                        size={17}
                      />
                      <span className={styles.releaseDate}>
                        {releaseDate ?
                          moment(releaseDate).format(shortDateFormat) :
                          translate('Unknown')
                        }
                      </span>
                    </div>
                  </Label>

                  <Tooltip
                    anchor={
                      <Label
                        className={styles.detailsLabel}
                        size={sizes.LARGE}
                      >
                        <div>
                          <Icon
                            name={icons.DRIVE}
                            size={17}
                          />
                          <span className={styles.sizeOnDisk}>
                            {formatBytes(sizeOnDisk)}
                          </span>
                        </div>
                      </Label>
                    }
                    tooltip={
                      <span>
                        {trackFilesCountMessage}
                      </span>
                    }
                    kind={kinds.INVERSE}
                    position={tooltipPositions.BOTTOM}
                  />

                  <Label
                    className={styles.detailsLabel}
                    size={sizes.LARGE}
                  >
                    <div>
                      <Icon
                        name={monitored ? icons.MONITORED : icons.UNMONITORED}
                        size={17}
                      />
                      <span className={styles.qualityProfileName}>
                        {monitored ? translate('Monitored') : translate('Unmonitored')}
                      </span>
                    </div>
                  </Label>

                  {
                    albumType ?
                      <Label
                        className={styles.detailsLabel}
                        title={translate('Type')}
                        size={sizes.LARGE}
                      >
                        <div>
                          <Icon
                            name={icons.INFO}
                            size={17}
                          />
                          <span className={styles.albumType}>
                            {albumType}
                          </span>
                        </div>
                      </Label> :
                      null
                  }

                  {
                    secondaryTypes.length ?
                      <Label
                        className={styles.detailsLabel}
                        title={translate('SecondaryTypes')}
                        size={sizes.LARGE}
                      >
                        <div>
                          <Icon
                            name={icons.INFO}
                            size={17}
                          />
                          <span className={styles.secondaryTypes}>
                            {secondaryTypes.join(', ')}
                          </span>
                        </div>
                      </Label> :
                      null
                  }

                  <Tooltip
                    anchor={
                      <Label
                        className={styles.detailsLabel}
                        size={sizes.LARGE}
                      >
                        <div>
                          <Icon
                            name={icons.EXTERNAL_LINK}
                            size={17}
                          />
                          <span className={styles.links}>
                            {translate('Links')}
                          </span>
                        </div>
                      </Label>
                    }
                    tooltip={
                      <AlbumDetailsLinks
                        foreignAlbumId={foreignAlbumId}
                        links={links}
                      />
                    }
                    kind={kinds.INVERSE}
                    position={tooltipPositions.BOTTOM}
                  />

                </div>
                <div className={styles.overview} title={overview}>
                  <TextTruncate
                    line={Math.floor(125 / (intermediateFontSize * lineHeight))}
                    text={overview}
                  />
                </div>
              </div>
            </div>
          </div>

          <div className={styles.contentContainer}>
            {
              !isPopulated && !albumsError && !tracksError && !trackFilesError ?
                <LoadingIndicator /> :
                null
            }

            {
              !isFetching && albumsError ?
                <Alert kind={kinds.DANGER}>
                  {translate('AlbumsLoadError')}
                </Alert> :
                null
            }

            {
              !isFetching && tracksError ?
                <Alert kind={kinds.DANGER}>
                  {translate('TracksLoadError')}
                </Alert> :
                null
            }

            {
              !isFetching && trackFilesError ?
                <Alert kind={kinds.DANGER}>
                  {translate('TrackFilesLoadError')}
                </Alert> :
                null
            }

            {
              isPopulated && !!media.length &&
                <div>
                  {
                    media.slice(0).map((medium) => {
                      return (
                        <AlbumDetailsMediumConnector
                          key={medium.mediumNumber}
                          albumId={id}
                          albumMonitored={monitored}
                          {...medium}
                          isExpanded={expandedState[medium.mediumNumber]}
                          onExpandPress={this.onExpandPress}
                        />
                      );
                    })
                  }
                </div>
            }

            {
              isPopulated && !media.length ?
                <Alert kind={kinds.WARNING}>
                  {translate('NoMediumInformation')}
                </Alert> :
                null
            }

          </div>

          <OrganizePreviewModalConnector
            isOpen={isOrganizeModalOpen}
            artistId={artist.id}
            albumId={id}
            onModalClose={this.onOrganizeModalClose}
          />

          <RetagPreviewModalConnector
            isOpen={isRetagModalOpen}
            artistId={artist.id}
            albumId={id}
            onModalClose={this.onRetagModalClose}
          />

          <TrackFileEditorModal
            isOpen={isManageTracksOpen}
            artistId={artist.id}
            albumId={id}
            onModalClose={this.onManageTracksModalClose}
          />

          <AlbumInteractiveSearchModalConnector
            isOpen={isInteractiveSearchModalOpen}
            albumId={id}
            albumTitle={title}
            onModalClose={this.onInteractiveSearchModalClose}
          />

          <ArtistHistoryModal
            isOpen={isArtistHistoryModalOpen}
            artistId={artist.id}
            albumId={id}
            onModalClose={this.onArtistHistoryModalClose}
          />

          <EditAlbumModalConnector
            isOpen={isEditAlbumModalOpen}
            albumId={id}
            artistId={artist.id}
            onModalClose={this.onEditAlbumModalClose}
            onDeleteArtistPress={this.onDeleteAlbumPress}
          />

          <DeleteAlbumModal
            isOpen={isDeleteAlbumModalOpen}
            albumId={id}
            foreignArtistId={artist.foreignArtistId}
            onModalClose={this.onDeleteAlbumModalClose}
          />

        </PageContentBody>
      </PageContent>
    );
  }
}

AlbumDetails.propTypes = {
  id: PropTypes.number.isRequired,
  foreignAlbumId: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  disambiguation: PropTypes.string,
  duration: PropTypes.number,
  overview: PropTypes.string,
  albumType: PropTypes.string.isRequired,
  secondaryTypes: PropTypes.arrayOf(PropTypes.string).isRequired,
  statistics: PropTypes.object.isRequired,
  releaseDate: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  genres: PropTypes.arrayOf(PropTypes.string).isRequired,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  media: PropTypes.arrayOf(PropTypes.object).isRequired,
  monitored: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSearching: PropTypes.bool,
  isFetching: PropTypes.bool,
  isPopulated: PropTypes.bool,
  albumsError: PropTypes.object,
  tracksError: PropTypes.object,
  trackFilesError: PropTypes.object,
  hasTrackFiles: PropTypes.bool.isRequired,
  artist: PropTypes.object,
  previousAlbum: PropTypes.object,
  nextAlbum: PropTypes.object,
  onMonitorTogglePress: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func,
  onSearchPress: PropTypes.func.isRequired
};

AlbumDetails.defaultProps = {
  secondaryTypes: [],
  statistics: {},
  isSaving: false
};

export default AlbumDetails;
