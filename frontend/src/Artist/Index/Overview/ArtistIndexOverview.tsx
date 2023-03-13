import React, { useCallback, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import TextTruncate from 'react-text-truncate';
import { Statistics } from 'Artist/Artist';
import ArtistPoster from 'Artist/ArtistPoster';
import DeleteArtistModal from 'Artist/Delete/DeleteArtistModal';
import EditArtistModalConnector from 'Artist/Edit/EditArtistModalConnector';
import ArtistIndexProgressBar from 'Artist/Index/ProgressBar/ArtistIndexProgressBar';
import ArtistIndexPosterSelect from 'Artist/Index/Select/ArtistIndexPosterSelect';
import { ARTIST_SEARCH, REFRESH_ARTIST } from 'Commands/commandNames';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import dimensions from 'Styles/Variables/dimensions';
import fonts from 'Styles/Variables/fonts';
import translate from 'Utilities/String/translate';
import createArtistIndexItemSelector from '../createArtistIndexItemSelector';
import ArtistIndexOverviewInfo from './ArtistIndexOverviewInfo';
import selectOverviewOptions from './selectOverviewOptions';
import styles from './ArtistIndexOverview.css';

const columnPadding = parseInt(dimensions.artistIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.artistIndexColumnPaddingSmallScreen
);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

// Hardcoded height based on line-height of 32 + bottom margin of 10.
// Less side-effecty than using react-measure.
const TITLE_HEIGHT = 42;

interface ArtistIndexOverviewProps {
  artistId: number;
  sortKey: string;
  posterWidth: number;
  posterHeight: number;
  rowHeight: number;
  isSelectMode: boolean;
  isSmallScreen: boolean;
}

function ArtistIndexOverview(props: ArtistIndexOverviewProps) {
  const {
    artistId,
    sortKey,
    posterWidth,
    posterHeight,
    rowHeight,
    isSelectMode,
    isSmallScreen,
  } = props;

  const { artist, qualityProfile, isRefreshingArtist, isSearchingArtist } =
    useSelector(createArtistIndexItemSelector(props.artistId));

  const overviewOptions = useSelector(selectOverviewOptions);

  const {
    artistName,
    monitored,
    status,
    path,
    foreignArtistId,
    nextAlbum,
    lastAlbum,
    added,
    overview,
    statistics = {} as Statistics,
    images,
  } = artist;

  const {
    albumCount = 0,
    sizeOnDisk = 0,
    trackCount = 0,
    trackFileCount = 0,
    totalTrackCount = 0,
  } = statistics;

  const dispatch = useDispatch();
  const [isEditArtistModalOpen, setIsEditArtistModalOpen] = useState(false);
  const [isDeleteArtistModalOpen, setIsDeleteArtistModalOpen] = useState(false);

  const onRefreshPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: REFRESH_ARTIST,
        artistId,
      })
    );
  }, [artistId, dispatch]);

  const onSearchPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: ARTIST_SEARCH,
        artistId,
      })
    );
  }, [artistId, dispatch]);

  const onEditArtistPress = useCallback(() => {
    setIsEditArtistModalOpen(true);
  }, [setIsEditArtistModalOpen]);

  const onEditArtistModalClose = useCallback(() => {
    setIsEditArtistModalOpen(false);
  }, [setIsEditArtistModalOpen]);

  const onDeleteArtistPress = useCallback(() => {
    setIsEditArtistModalOpen(false);
    setIsDeleteArtistModalOpen(true);
  }, [setIsDeleteArtistModalOpen]);

  const onDeleteArtistModalClose = useCallback(() => {
    setIsDeleteArtistModalOpen(false);
  }, [setIsDeleteArtistModalOpen]);

  const link = `/artist/${foreignArtistId}`;

  const elementStyle = {
    width: `${posterWidth}px`,
    height: `${posterHeight}px`,
  };

  const contentHeight = useMemo(() => {
    const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

    return rowHeight - padding;
  }, [rowHeight, isSmallScreen]);

  const overviewHeight = contentHeight - TITLE_HEIGHT;

  return (
    <div>
      <div className={styles.content}>
        <div className={styles.poster}>
          <div className={styles.posterContainer}>
            {isSelectMode ? (
              <ArtistIndexPosterSelect artistId={artistId} />
            ) : null}

            {status === 'ended' && (
              <div className={styles.ended} title={translate('Inactive')} />
            )}

            <Link className={styles.link} style={elementStyle} to={link}>
              <ArtistPoster
                className={styles.poster}
                style={elementStyle}
                images={images}
                size={250}
                lazy={false}
                overflow={true}
              />
            </Link>
          </div>

          <ArtistIndexProgressBar
            artistId={artistId}
            monitored={monitored}
            status={status}
            trackCount={trackCount}
            trackFileCount={trackFileCount}
            totalTrackCount={totalTrackCount}
            width={posterWidth}
            detailedProgressBar={overviewOptions.detailedProgressBar}
            isStandalone={false}
          />
        </div>

        <div className={styles.info} style={{ maxHeight: contentHeight }}>
          <div className={styles.titleRow}>
            <Link className={styles.title} to={link}>
              {artistName}
            </Link>

            <div className={styles.actions}>
              <SpinnerIconButton
                name={icons.REFRESH}
                title={translate('RefreshArtist')}
                isSpinning={isRefreshingArtist}
                onPress={onRefreshPress}
              />

              {overviewOptions.showSearchAction ? (
                <SpinnerIconButton
                  name={icons.SEARCH}
                  title={translate('SearchForMonitoredAlbums')}
                  isSpinning={isSearchingArtist}
                  onPress={onSearchPress}
                />
              ) : null}

              <IconButton
                name={icons.EDIT}
                title={translate('EditArtist')}
                onPress={onEditArtistPress}
              />
            </div>
          </div>

          <div className={styles.details}>
            <Link className={styles.overview} to={link}>
              <TextTruncate
                line={Math.floor(
                  overviewHeight / (defaultFontSize * lineHeight)
                )}
                text={overview}
              />
            </Link>

            <ArtistIndexOverviewInfo
              height={overviewHeight}
              monitored={monitored}
              nextAlbum={nextAlbum}
              lastAlbum={lastAlbum}
              added={added}
              albumCount={albumCount}
              qualityProfile={qualityProfile}
              sizeOnDisk={sizeOnDisk}
              path={path}
              sortKey={sortKey}
              {...overviewOptions}
            />
          </div>
        </div>
      </div>

      <EditArtistModalConnector
        isOpen={isEditArtistModalOpen}
        artistId={artistId}
        onModalClose={onEditArtistModalClose}
        onDeleteArtistPress={onDeleteArtistPress}
      />

      <DeleteArtistModal
        isOpen={isDeleteArtistModalOpen}
        artistId={artistId}
        onModalClose={onDeleteArtistModalClose}
      />
    </div>
  );
}

export default ArtistIndexOverview;
