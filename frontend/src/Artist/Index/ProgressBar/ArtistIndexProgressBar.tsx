import React from 'react';
import ProgressBar from 'Components/ProgressBar';
import { sizes } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Artist/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './ArtistIndexProgressBar.css';

interface ArtistIndexProgressBarProps {
  monitored: boolean;
  status: string;
  trackCount: number;
  trackFileCount: number;
  totalTrackCount: number;
  posterWidth: number;
  detailedProgressBar: boolean;
}

function ArtistIndexProgressBar(props: ArtistIndexProgressBarProps) {
  const {
    monitored,
    status,
    trackCount,
    trackFileCount,
    totalTrackCount,
    posterWidth,
    detailedProgressBar,
  } = props;

  const progress = trackCount ? (trackFileCount / trackCount) * 100 : 100;
  const text = `${trackFileCount} / ${trackCount}`;

  return (
    <ProgressBar
      className={styles.progressBar}
      containerClassName={styles.progress}
      progress={progress}
      kind={getProgressBarKind(status, monitored, progress)}
      size={detailedProgressBar ? sizes.MEDIUM : sizes.SMALL}
      showText={detailedProgressBar}
      text={text}
      title={translate('ArtistProgressBarText', {
        trackFileCount,
        trackCount,
        totalTrackCount,
      })}
      width={posterWidth}
    />
  );
}

export default ArtistIndexProgressBar;
