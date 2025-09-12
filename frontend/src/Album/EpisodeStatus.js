import PropTypes from 'prop-types';
import React from 'react';
import QueueDetails from 'Activity/Queue/QueueDetails';
import Icon from 'Components/Icon';
import ProgressBar from 'Components/ProgressBar';
import { icons, kinds, sizes } from 'Helpers/Props';
import isBefore from 'Utilities/Date/isBefore';
import translate from 'Utilities/String/translate';
import TrackQuality from './TrackQuality';
import styles from './EpisodeStatus.css';

function EpisodeStatus(props) {
  const {
    releaseDate,
    monitored,
    grabbed,
    queueItem,
    trackFile
  } = props;

  const hasTrackFile = !!trackFile;
  const isQueued = !!queueItem;
  const isReleased = isBefore(releaseDate);

  if (isQueued) {
    const {
      sizeleft,
      size
    } = queueItem;

    const progress = size ? (100 - sizeleft / size * 100) : 0;

    return (
      <div className={styles.center}>
        <QueueDetails
          {...queueItem}
          progressBar={
            <ProgressBar
              title={translate('AlbumIsDownloadingInterp', [progress.toFixed(1), queueItem.title])}
              progress={progress}
              kind={kinds.PURPLE}
              size={sizes.MEDIUM}
            />
          }
        />
      </div>
    );
  }

  if (grabbed) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.DOWNLOADING}
          title={translate('AlbumIsDownloading')}
        />
      </div>
    );
  }

  if (hasTrackFile) {
    const quality = trackFile.quality;
    const isCutoffNotMet = trackFile.qualityCutoffNotMet;

    return (
      <div className={styles.center}>
        <TrackQuality
          quality={quality}
          size={trackFile.size}
          isCutoffNotMet={isCutoffNotMet}
          title={translate('TrackDownloaded')}
        />
      </div>
    );
  }

  if (!releaseDate) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.TBA}
          title={translate('TBA')}
        />
      </div>
    );
  }

  if (!monitored) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.UNMONITORED}
          title={translate('AlbumIsNotMonitored')}
        />
      </div>
    );
  }

  if (isReleased) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.MISSING}
          title={translate('TrackMissingFromDisk')}
        />
      </div>
    );
  }

  return (
    <div className={styles.center}>
      <Icon
        name={icons.NOT_AIRED}
        title={translate('AlbumHasNotAired')}
      />
    </div>
  );
}

EpisodeStatus.propTypes = {
  releaseDate: PropTypes.string,
  monitored: PropTypes.bool,
  grabbed: PropTypes.bool,
  queueItem: PropTypes.object,
  trackFile: PropTypes.object
};

export default EpisodeStatus;
