import classNames from 'classnames';
import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { Statistics } from 'Album/Album';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import { toggleAlbumsMonitored } from 'Store/Actions/albumActions';
import translate from 'Utilities/String/translate';
import styles from './AlbumStudioAlbum.css';

interface AlbumStudioAlbumProps {
  artistId: number;
  albumId: number;
  title: string;
  disambiguation?: string;
  albumType: string;
  monitored: boolean;
  statistics: Statistics;
  isSaving: boolean;
}

function AlbumStudioAlbum(props: AlbumStudioAlbumProps) {
  const {
    albumId,
    title,
    disambiguation,
    albumType,
    monitored,
    statistics = {
      trackFileCount: 0,
      totalTrackCount: 0,
      percentOfTracks: 0,
    },
    isSaving = false,
  } = props;

  const {
    trackFileCount = 0,
    totalTrackCount = 0,
    percentOfTracks = 0,
  } = statistics;

  const dispatch = useDispatch();
  const onAlbumMonitoredPress = useCallback(() => {
    dispatch(
      toggleAlbumsMonitored({
        albumIds: [albumId],
        monitored: !monitored,
      })
    );
  }, [albumId, monitored, dispatch]);

  return (
    <div className={styles.album}>
      <div className={styles.info}>
        <MonitorToggleButton
          monitored={monitored}
          isSaving={isSaving}
          onPress={onAlbumMonitoredPress}
        />

        <span>
          {disambiguation ? `${title} (${disambiguation})` : `${title}`}
        </span>
      </div>

      <div className={styles.albumType}>
        <span>{albumType}</span>
      </div>

      <div
        className={classNames(
          styles.tracks,
          percentOfTracks < 100 && monitored && styles.missingWanted,
          percentOfTracks === 100 && styles.allTracks
        )}
        title={translate('AlbumStudioTracksDownloaded', {
          trackFileCount,
          totalTrackCount,
        })}
      >
        {totalTrackCount === 0 ? '0/0' : `${trackFileCount}/${totalTrackCount}`}
      </div>
    </div>
  );
}

export default AlbumStudioAlbum;
