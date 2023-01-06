import React from 'react';
import Album from 'Album/Album';
import TagListConnector from 'Components/TagListConnector';
import MetadataProfile from 'typings/MetadataProfile';
import QualityProfile from 'typings/QualityProfile';
import formatDateTime from 'Utilities/Date/formatDateTime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './ArtistIndexPosterInfo.css';

interface ArtistIndexPosterInfoProps {
  artistType?: string;
  showQualityProfile: boolean;
  qualityProfile?: QualityProfile;
  metadataProfile?: MetadataProfile;
  showNextAlbum: boolean;
  nextAlbum?: Album;
  lastAlbum?: Album;
  added?: string;
  albumCount: number;
  path: string;
  sizeOnDisk?: number;
  tags?: number[];
  sortKey: string;
  showRelativeDates: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  timeFormat: string;
}

function ArtistIndexPosterInfo(props: ArtistIndexPosterInfoProps) {
  const {
    artistType,
    qualityProfile,
    metadataProfile,
    showQualityProfile,
    showNextAlbum,
    nextAlbum,
    lastAlbum,
    added,
    albumCount,
    path,
    sizeOnDisk,
    tags,
    sortKey,
    showRelativeDates,
    shortDateFormat,
    longDateFormat,
    timeFormat,
  } = props;

  if (sortKey === 'artistType' && artistType) {
    return (
      <div className={styles.info} title={translate('ArtistType')}>
        {artistType}
      </div>
    );
  }

  if (
    sortKey === 'qualityProfileId' &&
    !showQualityProfile &&
    !!qualityProfile?.name
  ) {
    return (
      <div className={styles.info} title={translate('QualityProfile')}>
        {qualityProfile.name}
      </div>
    );
  }

  if (sortKey === 'metadataProfileId' && !!metadataProfile?.name) {
    return (
      <div className={styles.info} title={translate('MetadataProfile')}>
        {metadataProfile.name}
      </div>
    );
  }

  if (sortKey === 'nextAlbum' && !showNextAlbum && !!nextAlbum?.releaseDate) {
    return (
      <div
        className={styles.info}
        title={`${translate('NextAlbum')}: ${formatDateTime(
          nextAlbum.releaseDate,
          longDateFormat,
          timeFormat
        )}`}
      >
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
    );
  }

  if (sortKey === 'lastAlbum' && !!lastAlbum?.releaseDate) {
    return (
      <div
        className={styles.info}
        title={`${translate('LastAlbum')}: ${formatDateTime(
          lastAlbum.releaseDate,
          longDateFormat,
          timeFormat
        )}`}
      >
        {getRelativeDate(
          lastAlbum.releaseDate,
          shortDateFormat,
          showRelativeDates,
          {
            timeFormat,
            timeForToday: true,
          }
        )}
      </div>
    );
  }

  if (sortKey === 'added' && added) {
    const addedDate = getRelativeDate(
      added,
      shortDateFormat,
      showRelativeDates,
      {
        timeFormat,
        timeForToday: false,
      }
    );

    return (
      <div
        className={styles.info}
        title={formatDateTime(added, longDateFormat, timeFormat)}
      >
        {translate('Added')}: {addedDate}
      </div>
    );
  }

  if (sortKey === 'albumCount') {
    let albums = translate('OneAlbum');

    if (albumCount === 0) {
      albums = translate('NoAlbums');
    } else if (albumCount > 1) {
      albums = translate('CountAlbums', { albumCount });
    }

    return <div className={styles.info}>{albums}</div>;
  }

  if (sortKey === 'path') {
    return (
      <div className={styles.info} title={translate('Path')}>
        {path}
      </div>
    );
  }

  if (sortKey === 'sizeOnDisk') {
    return (
      <div className={styles.info} title={translate('SizeOnDisk')}>
        {formatBytes(sizeOnDisk)}
      </div>
    );
  }

  if (sortKey === 'tags' && tags) {
    return (
      <div className={styles.info} title={translate('Tags')}>
        <TagListConnector tags={tags} />
      </div>
    );
  }

  return null;
}

export default ArtistIndexPosterInfo;
