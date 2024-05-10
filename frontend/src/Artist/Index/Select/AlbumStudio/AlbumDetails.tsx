import _ from 'lodash';
import React, { useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Statistics } from 'Album/Album';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { kinds } from 'Helpers/Props';
import { clearAlbums, fetchAlbums } from 'Store/Actions/albumActions';
import createArtistAlbumsSelector from 'Store/Selectors/createArtistAlbumsSelector';
import translate from 'Utilities/String/translate';
import AlbumStudioAlbum from './AlbumStudioAlbum';
import styles from './AlbumDetails.css';

interface AlbumDetailsProps {
  artistId: number;
}

function AlbumDetails(props: AlbumDetailsProps) {
  const { artistId } = props;

  const {
    isFetching,
    isPopulated,
    error,
    items: albums,
  } = useSelector(createArtistAlbumsSelector(artistId));

  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(fetchAlbums({ artistId }));

    return () => {
      dispatch(clearAlbums());
    };
  }, [dispatch, artistId]);

  const latestAlbums = useMemo(() => {
    const sortedAlbums = _.orderBy(albums, 'releaseDate', 'desc');

    return sortedAlbums.slice(0, 20);
  }, [albums]);

  return (
    <div className={styles.albums}>
      {isFetching ? <LoadingIndicator /> : null}

      {!isFetching && error ? (
        <Alert kind={kinds.DANGER}>{translate('AlbumsLoadError')}</Alert>
      ) : null}

      {isPopulated && !error
        ? latestAlbums.map((album) => {
            const {
              id: albumId,
              title,
              disambiguation,
              albumType,
              monitored,
              statistics = {} as Statistics,
              isSaving = false,
            } = album;

            return (
              <AlbumStudioAlbum
                key={albumId}
                artistId={artistId}
                albumId={albumId}
                title={title}
                disambiguation={disambiguation}
                albumType={albumType}
                monitored={monitored}
                statistics={statistics}
                isSaving={isSaving}
              />
            );
          })
        : null}

      {latestAlbums.length < albums.length ? (
        <div className={styles.truncated}>
          {translate('AlbumStudioTruncated')}
        </div>
      ) : null}
    </div>
  );
}

export default AlbumDetails;
