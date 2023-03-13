import React from 'react';
import { useSelector } from 'react-redux';
import createArtistQueueItemsDetailsSelector, {
  ArtistQueueDetails,
} from 'Artist/Index/createArtistQueueDetailsSelector';
import ProgressBar from 'Components/ProgressBar';
import { sizes } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Artist/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './ArtistIndexProgressBar.css';

interface ArtistIndexProgressBarProps {
  artistId: number;
  monitored: boolean;
  status: string;
  trackCount: number;
  trackFileCount: number;
  totalTrackCount: number;
  width: number;
  detailedProgressBar: boolean;
  isStandalone: boolean;
}

function ArtistIndexProgressBar(props: ArtistIndexProgressBarProps) {
  const {
    artistId,
    monitored,
    status,
    trackCount,
    trackFileCount,
    totalTrackCount,
    width,
    detailedProgressBar,
    isStandalone,
  } = props;

  const queueDetails: ArtistQueueDetails = useSelector(
    createArtistQueueItemsDetailsSelector(artistId)
  );

  const newDownloads = queueDetails.count - queueDetails.tracksWithFiles;
  const progress = trackCount ? (trackFileCount / trackCount) * 100 : 100;
  const text = newDownloads
    ? `${trackFileCount} + ${newDownloads} / ${trackCount}`
    : `${trackFileCount} / ${trackCount}`;

  return (
    <ProgressBar
      className={styles.progressBar}
      containerClassName={isStandalone ? undefined : styles.progress}
      progress={progress}
      kind={getProgressBarKind(
        status,
        monitored,
        progress,
        queueDetails.count > 0
      )}
      size={detailedProgressBar ? sizes.MEDIUM : sizes.SMALL}
      showText={detailedProgressBar}
      text={text}
      title={translate('ArtistProgressBarText', {
        trackFileCount,
        trackCount,
        totalTrackCount,
        downloadingCount: queueDetails.count,
      })}
      width={width}
    />
  );
}

export default ArtistIndexProgressBar;
