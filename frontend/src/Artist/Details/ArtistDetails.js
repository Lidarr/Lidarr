import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import ArtistPoster from 'Artist/ArtistPoster';
import DeleteArtistModal from 'Artist/Delete/DeleteArtistModal';
import EditArtistModalConnector from 'Artist/Edit/EditArtistModalConnector';
import ArtistHistoryModal from 'Artist/History/ArtistHistoryModal';
import MonitoringOptionsModal from 'Artist/MonitoringOptions/MonitoringOptionsModal';
import ArtistInteractiveSearchModalConnector from 'Artist/Search/ArtistInteractiveSearchModalConnector';
import Alert from 'Components/Alert';
import HeartRating from 'Components/HeartRating';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import { align, icons, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
import RetagPreviewModalConnector from 'Retag/RetagPreviewModalConnector';
import QualityProfileNameConnector from 'Settings/Profiles/Quality/QualityProfileNameConnector';
import fonts from 'Styles/Variables/fonts';
import TrackFileEditorModal from 'TrackFile/Editor/TrackFileEditorModal';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import InteractiveImportModal from '../../InteractiveImport/InteractiveImportModal';
import ArtistAlternateTitles from './ArtistAlternateTitles';
import ArtistDetailsLinks from './ArtistDetailsLinks';
import ArtistDetailsSeasonConnector from './ArtistDetailsSeasonConnector';
import ArtistGenres from './ArtistGenres';
import ArtistTagsConnector from './ArtistTagsConnector';
import styles from './ArtistDetails.css';

const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

function getFanartUrl(images) {
  return _.find(images, { coverType: 'fanart' })?.url;
}

function getExpandedState(newState) {
  return {
    allExpanded: newState.allSelected,
    allCollapsed: newState.allUnselected,
    expandedState: newState.selectedState
  };
}

class ArtistDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isRetagModalOpen: false,
      isManageTracksOpen: false,
      isEditArtistModalOpen: false,
      isDeleteArtistModalOpen: false,
      isArtistHistoryModalOpen: false,
      isInteractiveImportModalOpen: false,
      isInteractiveSearchModalOpen: false,
      isMonitorOptionsModalOpen: false,
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

  onManageTracksPress = () => {
    this.setState({ isManageTracksOpen: true });
  };

  onManageTracksModalClose = () => {
    this.setState({ isManageTracksOpen: false });
  };

  onInteractiveImportPress = () => {
    this.setState({ isInteractiveImportModalOpen: true });
  };

  onInteractiveImportModalClose = () => {
    this.setState({ isInteractiveImportModalOpen: false });
  };

  onInteractiveSearchPress = () => {
    this.setState({ isInteractiveSearchModalOpen: true });
  };

  onInteractiveSearchModalClose = () => {
    this.setState({ isInteractiveSearchModalOpen: false });
  };

  onEditArtistPress = () => {
    this.setState({ isEditArtistModalOpen: true });
  };

  onEditArtistModalClose = () => {
    this.setState({ isEditArtistModalOpen: false });
  };

  onDeleteArtistPress = () => {
    this.setState({
      isEditArtistModalOpen: false,
      isDeleteArtistModalOpen: true
    });
  };

  onDeleteArtistModalClose = () => {
    this.setState({ isDeleteArtistModalOpen: false });
  };

  onArtistHistoryPress = () => {
    this.setState({ isArtistHistoryModalOpen: true });
  };

  onArtistHistoryModalClose = () => {
    this.setState({ isArtistHistoryModalOpen: false });
  };

  onMonitorOptionsPress = () => {
    this.setState({ isMonitorOptionsModalOpen: true });
  };

  onMonitorOptionsClose = () => {
    this.setState({ isMonitorOptionsModalOpen: false });
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
      foreignArtistId,
      artistName,
      ratings,
      path,
      statistics = {},
      qualityProfileId,
      monitored,
      genres,
      albumTypes,
      status,
      overview,
      links,
      images,
      artistType,
      alternateTitles,
      tags,
      isSaving,
      isRefreshing,
      isSearching,
      isFetching,
      isPopulated,
      albumsError,
      trackFilesError,
      hasAlbums,
      hasMonitoredAlbums,
      hasTrackFiles,
      previousArtist,
      nextArtist,
      onMonitorTogglePress,
      onRefreshPress,
      onSearchPress
    } = this.props;

    const {
      trackFileCount = 0,
      sizeOnDisk = 0
    } = statistics;

    const {
      isOrganizeModalOpen,
      isRetagModalOpen,
      isManageTracksOpen,
      isEditArtistModalOpen,
      isDeleteArtistModalOpen,
      isArtistHistoryModalOpen,
      isInteractiveImportModalOpen,
      isInteractiveSearchModalOpen,
      isMonitorOptionsModalOpen,
      allExpanded,
      allCollapsed,
      expandedState
    } = this.state;

    const continuing = status === 'continuing';
    const endedString = artistType === 'Person' ? translate('Deceased') : translate('Inactive');

    let trackFilesCountMessage = translate('TrackFilesCountMessage');

    if (trackFileCount === 1) {
      trackFilesCountMessage = '1 track file';
    } else if (trackFileCount > 1) {
      trackFilesCountMessage = `${trackFileCount} track files`;
    }

    let expandIcon = icons.EXPAND_INDETERMINATE;

    if (allExpanded) {
      expandIcon = icons.COLLAPSE;
    } else if (allCollapsed) {
      expandIcon = icons.EXPAND;
    }

    const fanartUrl = getFanartUrl(images);

    return (
      <PageContent title={artistName}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('RefreshScan')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              title={translate('RefreshInformationAndScanDisk')}
              isSpinning={isRefreshing}
              onPress={onRefreshPress}
            />

            <PageToolbarButton
              label={translate('SearchMonitored')}
              iconName={icons.SEARCH}
              isDisabled={!monitored || !hasMonitoredAlbums || !hasAlbums}
              isSpinning={isSearching}
              title={hasMonitoredAlbums ? undefined : translate('HasMonitoredAlbumsNoMonitoredAlbumsForThisArtist')}
              onPress={onSearchPress}
            />

            <PageToolbarButton
              label={translate('InteractiveSearch')}
              iconName={icons.INTERACTIVE}
              isDisabled={!monitored || !hasMonitoredAlbums || !hasAlbums}
              isSpinning={isSearching}
              title={hasMonitoredAlbums ? undefined : translate('HasMonitoredAlbumsNoMonitoredAlbumsForThisArtist')}
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
              isDisabled={!hasAlbums}
              onPress={this.onArtistHistoryPress}
            />

            <PageToolbarButton
              label={translate('ManualImport')}
              iconName={icons.INTERACTIVE}
              onPress={this.onInteractiveImportPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('ArtistMonitoring')}
              iconName={icons.MONITORED}
              onPress={this.onMonitorOptionsPress}
            />

            <PageToolbarButton
              label={translate('Edit')}
              iconName={icons.EDIT}
              onPress={this.onEditArtistPress}
            />

            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              onPress={this.onDeleteArtistPress}
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
              <ArtistPoster
                className={styles.poster}
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

                    <div className={styles.title}>
                      {artistName}
                    </div>

                    {
                      !!alternateTitles.length &&
                        <div className={styles.alternateTitlesIconContainer}>
                          <Popover
                            anchor={
                              <Icon
                                name={icons.ALTERNATE_TITLES}
                                size={20}
                              />
                            }
                            title={translate('AlternateTitles')}
                            body={<ArtistAlternateTitles alternateTitles={alternateTitles} />}
                            position={tooltipPositions.BOTTOM}
                          />
                        </div>
                    }
                  </div>

                  <div className={styles.artistNavigationButtons}>
                    <IconButton
                      className={styles.artistNavigationButton}
                      name={icons.ARROW_LEFT}
                      size={30}
                      title={translate('GoToInterp', [previousArtist.artistName])}
                      to={`/artist/${previousArtist.foreignArtistId}`}
                    />

                    <IconButton
                      className={styles.artistNavigationButton}
                      name={icons.ARROW_UP}
                      size={30}
                      title={translate('GoToArtistListing')}
                      to={'/'}
                    />

                    <IconButton
                      className={styles.artistNavigationButton}
                      name={icons.ARROW_RIGHT}
                      size={30}
                      title={translate('GoToInterp', [nextArtist.artistName])}
                      to={`/artist/${nextArtist.foreignArtistId}`}
                    />
                  </div>
                </div>

                <div className={styles.details}>
                  <div>
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
                    size={sizes.LARGE}
                  >
                    <Icon
                      name={icons.FOLDER}
                      size={17}
                    />

                    <span className={styles.path}>
                      {path}
                    </span>
                  </Label>

                  <Tooltip
                    anchor={
                      <Label
                        className={styles.detailsLabel}
                        size={sizes.LARGE}
                      >
                        <Icon
                          name={icons.DRIVE}
                          size={17}
                        />

                        <span className={styles.sizeOnDisk}>
                          {
                            formatBytes(sizeOnDisk || 0)
                          }
                        </span>
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
                    title={translate('QualityProfile')}
                    size={sizes.LARGE}
                  >
                    <Icon
                      name={icons.PROFILE}
                      size={17}
                    />

                    <span className={styles.qualityProfileName}>
                      {
                        <QualityProfileNameConnector
                          qualityProfileId={qualityProfileId}
                        />
                      }
                    </span>
                  </Label>

                  <Label
                    className={styles.detailsLabel}
                    size={sizes.LARGE}
                  >
                    <Icon
                      name={monitored ? icons.MONITORED : icons.UNMONITORED}
                      size={17}
                    />

                    <span className={styles.qualityProfileName}>
                      {monitored ? translate('Monitored') : translate('Unmonitored')}
                    </span>
                  </Label>

                  <Label
                    className={styles.detailsLabel}
                    title={continuing ? translate('ContinuingMoreAlbumsAreExpected') : translate('ContinuingNoAdditionalAlbumsAreExpected')}
                    size={sizes.LARGE}
                  >
                    <Icon
                      name={continuing ? icons.ARTIST_CONTINUING : icons.ARTIST_ENDED}
                      size={17}
                    />

                    <span className={styles.qualityProfileName}>
                      {continuing ? translate('Continuing') : endedString}
                    </span>
                  </Label>

                  <Tooltip
                    anchor={
                      <Label
                        className={styles.detailsLabel}
                        size={sizes.LARGE}
                      >
                        <Icon
                          name={icons.EXTERNAL_LINK}
                          size={17}
                        />

                        <span className={styles.links}>
                          {translate('Links')}
                        </span>
                      </Label>
                    }
                    tooltip={
                      <ArtistDetailsLinks
                        foreignArtistId={foreignArtistId}
                        links={links}
                      />
                    }
                    kind={kinds.INVERSE}
                    position={tooltipPositions.BOTTOM}
                  />

                  {
                    !!tags.length &&
                      <Tooltip
                        anchor={
                          <Label
                            className={styles.detailsLabel}
                            size={sizes.LARGE}
                          >
                            <Icon
                              name={icons.TAGS}
                              size={17}
                            />

                            <span className={styles.tags}>
                              Tags
                            </span>
                          </Label>
                        }
                        tooltip={<ArtistTagsConnector artistId={id} />}
                        kind={kinds.INVERSE}
                        position={tooltipPositions.BOTTOM}
                      />

                  }
                </div>
                <div className={styles.overview}>
                  <TextTruncate
                    line={Math.floor(125 / (defaultFontSize * lineHeight))}
                    text={overview}
                  />
                </div>
              </div>
            </div>
          </div>

          <div className={styles.contentContainer}>
            {
              !isPopulated && !albumsError && !trackFilesError &&
                <LoadingIndicator />
            }

            {
              !isFetching && albumsError ?
                <Alert kind={kinds.DANGER}>
                  {translate('AlbumsLoadError')}
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
              isPopulated && !!albumTypes.length &&
                <div>
                  {
                    albumTypes.slice(0).map((albumType) => {
                      return (
                        <ArtistDetailsSeasonConnector
                          key={albumType}
                          artistId={id}
                          name={albumType}
                          label={albumType}
                          {...albumType}
                          isExpanded={expandedState[albumType]}
                          onExpandPress={this.onExpandPress}
                        />
                      );
                    })
                  }
                </div>
            }

          </div>

          <div className={styles.metadataMessage}>
            Missing Albums, Singles, or Other Types? Modify or create a new
            <Link to='/settings/profiles'> Metadata Profile </Link>
            or manually
            <Link to={`/add/search?term=${encodeURIComponent(artistName)}`}> Search </Link>
            for new items!
          </div>

          <OrganizePreviewModalConnector
            isOpen={isOrganizeModalOpen}
            artistId={id}
            onModalClose={this.onOrganizeModalClose}
          />

          <RetagPreviewModalConnector
            isOpen={isRetagModalOpen}
            artistId={id}
            onModalClose={this.onRetagModalClose}
          />

          <TrackFileEditorModal
            isOpen={isManageTracksOpen}
            artistId={id}
            onModalClose={this.onManageTracksModalClose}
          />

          <ArtistHistoryModal
            isOpen={isArtistHistoryModalOpen}
            artistId={id}
            onModalClose={this.onArtistHistoryModalClose}
          />

          <EditArtistModalConnector
            isOpen={isEditArtistModalOpen}
            artistId={id}
            onModalClose={this.onEditArtistModalClose}
            onDeleteArtistPress={this.onDeleteArtistPress}
          />

          <DeleteArtistModal
            isOpen={isDeleteArtistModalOpen}
            artistId={id}
            onModalClose={this.onDeleteArtistModalClose}
          />

          <InteractiveImportModal
            isOpen={isInteractiveImportModalOpen}
            artistId={id}
            folder={path}
            allowArtistChange={false}
            showFilterExistingFiles={true}
            showImportMode={false}
            onModalClose={this.onInteractiveImportModalClose}
          />

          <ArtistInteractiveSearchModalConnector
            isOpen={isInteractiveSearchModalOpen}
            artistId={id}
            onModalClose={this.onInteractiveSearchModalClose}
          />

          <MonitoringOptionsModal
            isOpen={isMonitorOptionsModalOpen}
            artistId={id}
            onModalClose={this.onMonitorOptionsClose}
          />
        </PageContentBody>
      </PageContent>
    );
  }
}

ArtistDetails.propTypes = {
  id: PropTypes.number.isRequired,
  foreignArtistId: PropTypes.string.isRequired,
  artistName: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  artistType: PropTypes.string,
  albumTypes: PropTypes.arrayOf(PropTypes.string),
  genres: PropTypes.arrayOf(PropTypes.string),
  status: PropTypes.string.isRequired,
  overview: PropTypes.string.isRequired,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  alternateTitles: PropTypes.arrayOf(PropTypes.string).isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool.isRequired,
  isSearching: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  albumsError: PropTypes.object,
  trackFilesError: PropTypes.object,
  hasAlbums: PropTypes.bool.isRequired,
  hasMonitoredAlbums: PropTypes.bool.isRequired,
  hasTrackFiles: PropTypes.bool.isRequired,
  previousArtist: PropTypes.object.isRequired,
  nextArtist: PropTypes.object.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired
};

ArtistDetails.defaultProps = {
  statistics: {},
  tags: [],
  isSaving: false
};

export default ArtistDetails;
