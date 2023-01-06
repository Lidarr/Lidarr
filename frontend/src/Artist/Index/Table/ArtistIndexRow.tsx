import classNames from 'classnames';
import React, { useCallback, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AlbumTitleLink from 'Album/AlbumTitleLink';
import { Statistics } from 'Artist/Artist';
import ArtistBanner from 'Artist/ArtistBanner';
import ArtistNameLink from 'Artist/ArtistNameLink';
import DeleteArtistModal from 'Artist/Delete/DeleteArtistModal';
import EditArtistModalConnector from 'Artist/Edit/EditArtistModalConnector';
import createArtistIndexItemSelector from 'Artist/Index/createArtistIndexItemSelector';
import ArtistStatusCell from 'Artist/Index/Table/ArtistStatusCell';
import { ARTIST_SEARCH, REFRESH_ARTIST } from 'Commands/commandNames';
import HeartRating from 'Components/HeartRating';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import ProgressBar from 'Components/ProgressBar';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import Column from 'Components/Table/Column';
import TagListConnector from 'Components/TagListConnector';
import { icons } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import getProgressBarKind from 'Utilities/Artist/getProgressBarKind';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import hasGrowableColumns from './hasGrowableColumns';
import selectTableOptions from './selectTableOptions';
import styles from './ArtistIndexRow.css';

interface ArtistIndexRowProps {
  artistId: number;
  sortKey: string;
  columns: Column[];
}

function ArtistIndexRow(props: ArtistIndexRowProps) {
  const { artistId, columns } = props;

  const {
    artist,
    qualityProfile,
    metadataProfile,
    isRefreshingArtist,
    isSearchingArtist,
  } = useSelector(createArtistIndexItemSelector(props.artistId));

  const { showBanners, showSearchAction } = useSelector(selectTableOptions);

  const {
    artistName,
    foreignArtistId,
    monitored,
    status,
    path,
    nextAlbum,
    lastAlbum,
    added,
    statistics = {} as Statistics,
    images,
    artistType,
    genres = [],
    ratings,
    tags = [],
    isSaving = false,
  } = artist;

  const {
    albumCount = 0,
    trackCount = 0,
    trackFileCount = 0,
    totalTrackCount = 0,
    sizeOnDisk = 0,
  } = statistics;

  const dispatch = useDispatch();
  const [hasBannerError, setHasBannerError] = useState(false);
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

  const onBannerLoadError = useCallback(() => {
    setHasBannerError(true);
  }, [setHasBannerError]);

  const onBannerLoad = useCallback(() => {
    setHasBannerError(false);
  }, [setHasBannerError]);

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

  return (
    <>
      {columns.map((column) => {
        const { name, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'status') {
          return (
            <ArtistStatusCell
              key={name}
              className={styles[name]}
              artistId={artistId}
              artistType={artistType}
              monitored={monitored}
              status={status}
              isSaving={isSaving}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'sortName') {
          return (
            <VirtualTableRowCell
              key={name}
              className={classNames(
                styles[name],
                showBanners && styles.banner,
                showBanners && !hasGrowableColumns(columns) && styles.bannerGrow
              )}
            >
              {showBanners ? (
                <Link className={styles.link} to={`/artist/${foreignArtistId}`}>
                  <ArtistBanner
                    className={styles.bannerImage}
                    images={images}
                    lazy={false}
                    overflow={true}
                    onError={onBannerLoadError}
                    onLoad={onBannerLoad}
                  />

                  {hasBannerError && (
                    <div className={styles.overlayTitle}>{artistName}</div>
                  )}
                </Link>
              ) : (
                <ArtistNameLink
                  foreignArtistId={foreignArtistId}
                  artistName={artistName}
                />
              )}
            </VirtualTableRowCell>
          );
        }

        if (name === 'artistType') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {artistType}
            </VirtualTableRowCell>
          );
        }

        if (name === 'qualityProfileId') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {qualityProfile.name}
            </VirtualTableRowCell>
          );
        }

        if (name === 'qualityProfileId') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {qualityProfile.name}
            </VirtualTableRowCell>
          );
        }

        if (name === 'metadataProfileId') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {metadataProfile.name}
            </VirtualTableRowCell>
          );
        }

        if (name === 'nextAlbum') {
          if (nextAlbum) {
            return (
              <VirtualTableRowCell key={name} className={styles[name]}>
                <AlbumTitleLink
                  title={nextAlbum.title}
                  disambiguation={nextAlbum.disambiguation}
                  foreignAlbumId={nextAlbum.foreignAlbumId}
                />
              </VirtualTableRowCell>
            );
          }
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              None
            </VirtualTableRowCell>
          );
        }

        if (name === 'lastAlbum') {
          if (lastAlbum) {
            return (
              <VirtualTableRowCell key={name} className={styles[name]}>
                <AlbumTitleLink
                  title={lastAlbum.title}
                  disambiguation={lastAlbum.disambiguation}
                  foreignAlbumId={lastAlbum.foreignAlbumId}
                />
              </VirtualTableRowCell>
            );
          }
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              None
            </VirtualTableRowCell>
          );
        }

        if (name === 'added') {
          return (
            <RelativeDateCellConnector
              key={name}
              className={styles[name]}
              date={added}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'albumCount') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {albumCount}
            </VirtualTableRowCell>
          );
        }

        if (name === 'trackProgress') {
          const progress = trackCount
            ? (trackFileCount / trackCount) * 100
            : 100;

          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <ProgressBar
                progress={progress}
                kind={getProgressBarKind(status, monitored, progress)}
                showText={true}
                text={`${trackFileCount} / ${trackCount}`}
                title={translate('ArtistProgressBarText', {
                  trackFileCount,
                  trackCount,
                  totalTrackCount,
                })}
                width={125}
              />
            </VirtualTableRowCell>
          );
        }

        if (name === 'trackCount') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {totalTrackCount}
            </VirtualTableRowCell>
          );
        }

        if (name === 'path') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {path}
            </VirtualTableRowCell>
          );
        }

        if (name === 'sizeOnDisk') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {formatBytes(sizeOnDisk)}
            </VirtualTableRowCell>
          );
        }

        if (name === 'genres') {
          const joinedGenres = genres.join(', ');

          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <span title={joinedGenres}>{joinedGenres}</span>
            </VirtualTableRowCell>
          );
        }

        if (name === 'ratings') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <HeartRating rating={ratings.value} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'tags') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <TagListConnector tags={tags} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'actions') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <SpinnerIconButton
                name={icons.REFRESH}
                title={translate('RefreshArtist')}
                isSpinning={isRefreshingArtist}
                onPress={onRefreshPress}
              />

              {showSearchAction ? (
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
            </VirtualTableRowCell>
          );
        }

        return null;
      })}

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
    </>
  );
}

export default ArtistIndexRow;
