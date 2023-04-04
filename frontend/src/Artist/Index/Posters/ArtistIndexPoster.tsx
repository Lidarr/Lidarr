import React, { useCallback, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Statistics } from 'Artist/Artist';
import ArtistPoster from 'Artist/ArtistPoster';
import DeleteArtistModal from 'Artist/Delete/DeleteArtistModal';
import EditArtistModalConnector from 'Artist/Edit/EditArtistModalConnector';
import createArtistIndexItemSelector from 'Artist/Index/createArtistIndexItemSelector';
import ArtistIndexPosterInfo from 'Artist/Index/Posters/ArtistIndexPosterInfo';
import ArtistIndexProgressBar from 'Artist/Index/ProgressBar/ArtistIndexProgressBar';
import ArtistIndexPosterSelect from 'Artist/Index/Select/ArtistIndexPosterSelect';
import { ARTIST_SEARCH, REFRESH_ARTIST } from 'Commands/commandNames';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import translate from 'Utilities/String/translate';
import selectPosterOptions from './selectPosterOptions';
import styles from './ArtistIndexPoster.css';

interface ArtistIndexPosterProps {
  artistId: number;
  sortKey: string;
  isSelectMode: boolean;
  posterWidth: number;
  posterHeight: number;
}

function ArtistIndexPoster(props: ArtistIndexPosterProps) {
  const { artistId, sortKey, isSelectMode, posterWidth, posterHeight } = props;

  const {
    artist,
    qualityProfile,
    metadataProfile,
    isRefreshingArtist,
    isSearchingArtist,
  } = useSelector(createArtistIndexItemSelector(props.artistId));

  const {
    detailedProgressBar,
    showTitle,
    showMonitored,
    showQualityProfile,
    showNextAlbum,
    showSearchAction,
  } = useSelector(selectPosterOptions);

  const { showRelativeDates, shortDateFormat, longDateFormat, timeFormat } =
    useSelector(createUISettingsSelector());

  const {
    artistName,
    artistType,
    monitored,
    status,
    path,
    foreignArtistId,
    nextAlbum,
    added,
    statistics = {} as Statistics,
    images,
    tags,
  } = artist;

  const {
    albumCount = 0,
    trackCount = 0,
    trackFileCount = 0,
    totalTrackCount = 0,
    sizeOnDisk = 0,
  } = statistics;

  const dispatch = useDispatch();
  const [hasPosterError, setHasPosterError] = useState(false);
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

  const onPosterLoadError = useCallback(() => {
    setHasPosterError(true);
  }, [setHasPosterError]);

  const onPosterLoad = useCallback(() => {
    setHasPosterError(false);
  }, [setHasPosterError]);

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

  return (
    <div className={styles.content}>
      <div className={styles.posterContainer}>
        {isSelectMode ? <ArtistIndexPosterSelect artistId={artistId} /> : null}

        <Label className={styles.controls}>
          <SpinnerIconButton
            className={styles.action}
            name={icons.REFRESH}
            itle={translate('RefreshArtist')}
            isSpinning={isRefreshingArtist}
            onPress={onRefreshPress}
          />

          {showSearchAction ? (
            <SpinnerIconButton
              className={styles.action}
              name={icons.SEARCH}
              title={translate('SearchForMonitoredAlbums')}
              isSpinning={isSearchingArtist}
              onPress={onSearchPress}
            />
          ) : null}

          <IconButton
            className={styles.action}
            name={icons.EDIT}
            title={translate('EditArtist')}
            onPress={onEditArtistPress}
          />
        </Label>

        {status === 'ended' ? (
          <div className={styles.ended} title={translate('Inactive')} />
        ) : null}

        <Link className={styles.link} style={elementStyle} to={link}>
          <ArtistPoster
            style={elementStyle}
            images={images}
            size={250}
            lazy={false}
            overflow={true}
            onError={onPosterLoadError}
            onLoad={onPosterLoad}
          />

          {hasPosterError ? (
            <div className={styles.overlayTitle}>{artistName}</div>
          ) : null}
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
        detailedProgressBar={detailedProgressBar}
        isStandalone={false}
      />

      {showTitle ? (
        <div className={styles.title} title={artistName}>
          {artistName}
        </div>
      ) : null}

      {showMonitored ? (
        <div className={styles.title}>
          {monitored ? translate('Monitored') : translate('Unmonitored')}
        </div>
      ) : null}

      {showQualityProfile && !!qualityProfile?.name ? (
        <div className={styles.title} title={translate('QualityProfile')}>
          {qualityProfile.name}
        </div>
      ) : null}

      {showNextAlbum && !!nextAlbum?.releaseDate ? (
        <div className={styles.nextAlbum} title={translate('NextAlbum')}>
          {getRelativeDate(
            nextAlbum.releaseDate,
            shortDateFormat,
            showRelativeDates,
            {
              timeFormat,
              timeForToday: true,
            }
          )}
        </div>
      ) : null}

      <ArtistIndexPosterInfo
        artistType={artistType}
        added={added}
        albumCount={albumCount}
        sizeOnDisk={sizeOnDisk}
        path={path}
        tags={tags}
        qualityProfile={qualityProfile}
        metadataProfile={metadataProfile}
        showQualityProfile={showQualityProfile}
        showNextAlbum={showNextAlbum}
        showRelativeDates={showRelativeDates}
        sortKey={sortKey}
        shortDateFormat={shortDateFormat}
        longDateFormat={longDateFormat}
        timeFormat={timeFormat}
      />

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

export default ArtistIndexPoster;
