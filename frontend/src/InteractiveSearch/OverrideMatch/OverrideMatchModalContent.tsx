import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Album from 'Album/Album';
import TrackQuality from 'Album/TrackQuality';
import Artist from 'Artist/Artist';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';
import usePrevious from 'Helpers/Hooks/usePrevious';
import ReleaseAlbum from 'InteractiveSearch/ReleaseAlbum';
import { QualityModel } from 'Quality/Quality';
import { grabRelease } from 'Store/Actions/releaseActions';
import { fetchDownloadClients } from 'Store/Actions/settingsActions';
import { createArtistSelectorForHook } from 'Store/Selectors/createArtistSelector';
import createEnabledDownloadClientsSelector from 'Store/Selectors/createEnabledDownloadClientsSelector';
import translate from 'Utilities/String/translate';
import SelectDownloadClientModal from './DownloadClient/SelectDownloadClientModal';
import SelectAlbumModal from './InterarctiveSearch/Album/SelectAlbumModal';
import SelectArtistModal from './InterarctiveSearch/Artist/SelectArtistModal';
import SelectQualityModal from './InterarctiveSearch/Quality/SelectQualityModal';
import OverrideMatchData from './OverrideMatchData';
import styles from './OverrideMatchModalContent.css';

type SelectType =
  | 'select'
  | 'artist'
  | 'album'
  | 'quality'
  | 'language'
  | 'downloadClient';

interface SelectedAlbum {
  id: number;
  albums: Album[];
}

interface OverrideMatchModalContentProps {
  indexerId: number;
  title: string;
  guid: string;
  artistId?: number;
  albums: ReleaseAlbum[];
  quality: QualityModel;
  protocol: DownloadProtocol;
  isGrabbing: boolean;
  grabError?: string;
  onModalClose(): void;
}

function OverrideMatchModalContent(props: OverrideMatchModalContentProps) {
  const modalTitle = translate('ManualGrab');
  const {
    indexerId,
    title,
    guid,
    protocol,
    isGrabbing,
    grabError,
    onModalClose,
  } = props;

  const [artistId, setArtistId] = useState(props.artistId);
  const [albums, setAlbums] = useState(props.albums);
  const [quality, setQuality] = useState(props.quality);
  const [downloadClientId, setDownloadClientId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectModalOpen, setSelectModalOpen] = useState<SelectType | null>(
    null
  );
  const previousIsGrabbing = usePrevious(isGrabbing);

  const dispatch = useDispatch();
  const artist: Artist | undefined = useSelector(
    createArtistSelectorForHook(artistId)
  );
  const { items: downloadClients } = useSelector(
    createEnabledDownloadClientsSelector(protocol)
  );

  const albumInfo = useMemo(() => {
    return albums.map((album) => {
      return <div key={album.id}>{album.title}</div>;
    });
  }, [albums]);

  const onSelectModalClose = useCallback(() => {
    setSelectModalOpen(null);
  }, [setSelectModalOpen]);

  const onSelectArtistPress = useCallback(() => {
    setSelectModalOpen('artist');
  }, [setSelectModalOpen]);

  const onArtistSelect = useCallback(
    (s: Artist) => {
      setArtistId(s.id);
      setAlbums([]);
      setSelectModalOpen(null);
    },
    [setArtistId, setAlbums, setSelectModalOpen]
  );

  const onSelectAlbumPress = useCallback(() => {
    setSelectModalOpen('album');
  }, [setSelectModalOpen]);

  const onAlbumsSelect = useCallback(
    (albumMap: SelectedAlbum[]) => {
      setAlbums(albumMap[0].albums);
      setSelectModalOpen(null);
    },
    [setAlbums, setSelectModalOpen]
  );

  const onSelectQualityPress = useCallback(() => {
    setSelectModalOpen('quality');
  }, [setSelectModalOpen]);

  const onQualitySelect = useCallback(
    (quality: QualityModel) => {
      setQuality(quality);
      setSelectModalOpen(null);
    },
    [setQuality, setSelectModalOpen]
  );

  const onSelectDownloadClientPress = useCallback(() => {
    setSelectModalOpen('downloadClient');
  }, [setSelectModalOpen]);

  const onDownloadClientSelect = useCallback(
    (downloadClientId: number) => {
      setDownloadClientId(downloadClientId);
      setSelectModalOpen(null);
    },
    [setDownloadClientId, setSelectModalOpen]
  );

  const onGrabPress = useCallback(() => {
    if (!artistId) {
      setError(translate('OverrideGrabNoArtist'));
      return;
    } else if (!albums.length) {
      setError(translate('OverrideGrabNoAlbum'));
      return;
    } else if (!quality) {
      setError(translate('OverrideGrabNoQuality'));
      return;
    }

    dispatch(
      grabRelease({
        indexerId,
        guid,
        artistId,
        albumsIds: albums.map((a) => a.id),
        quality,
        downloadClientId,
        shouldOverride: true,
      })
    );
  }, [
    indexerId,
    guid,
    artistId,
    albums,
    quality,
    downloadClientId,
    setError,
    dispatch,
  ]);

  useEffect(() => {
    if (!isGrabbing && previousIsGrabbing) {
      onModalClose();
    }
  }, [isGrabbing, previousIsGrabbing, onModalClose]);

  useEffect(
    () => {
      dispatch(fetchDownloadClients());
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    []
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('OverrideGrabModalTitle', { title })}
      </ModalHeader>

      <ModalBody>
        <DescriptionList>
          <DescriptionListItem
            className={styles.item}
            title={translate('Artist')}
            data={
              <OverrideMatchData
                value={artist?.artistName}
                onPress={onSelectArtistPress}
              />
            }
          />

          <DescriptionListItem
            className={styles.item}
            title={translate('Albums')}
            data={
              <OverrideMatchData
                value={albumInfo}
                isDisabled={!artist}
                onPress={onSelectAlbumPress}
              />
            }
          />

          <DescriptionListItem
            className={styles.item}
            title={translate('Quality')}
            data={
              <OverrideMatchData
                value={
                  <TrackQuality className={styles.label} quality={quality} />
                }
                onPress={onSelectQualityPress}
              />
            }
          />

          {downloadClients.length > 1 ? (
            <DescriptionListItem
              className={styles.item}
              title={translate('DownloadClient')}
              data={
                <OverrideMatchData
                  value={
                    downloadClients.find(
                      (downloadClient) => downloadClient.id === downloadClientId
                    )?.name ?? translate('Default')
                  }
                  onPress={onSelectDownloadClientPress}
                />
              }
            />
          ) : null}
        </DescriptionList>
      </ModalBody>

      <ModalFooter className={styles.footer}>
        <div className={styles.error}>{error || grabError}</div>

        <div className={styles.buttons}>
          <Button onPress={onModalClose}>{translate('Cancel')}</Button>

          <SpinnerErrorButton
            isSpinning={isGrabbing}
            error={grabError}
            onPress={onGrabPress}
          >
            {translate('GrabRelease')}
          </SpinnerErrorButton>
        </div>
      </ModalFooter>

      <SelectArtistModal
        isOpen={selectModalOpen === 'artist'}
        modalTitle={modalTitle}
        onArtistSelect={onArtistSelect}
        onModalClose={onSelectModalClose}
      />

      <SelectAlbumModal
        isOpen={selectModalOpen === 'album'}
        selectedIds={[guid]}
        artistId={artistId}
        selectedDetails={title}
        modalTitle={modalTitle}
        onAlbumsSelect={onAlbumsSelect}
        onModalClose={onSelectModalClose}
      />

      <SelectQualityModal
        isOpen={selectModalOpen === 'quality'}
        qualityId={quality ? quality.quality.id : 0}
        proper={quality ? quality.revision.version > 1 : false}
        real={quality ? quality.revision.real > 0 : false}
        modalTitle={modalTitle}
        onQualitySelect={onQualitySelect}
        onModalClose={onSelectModalClose}
      />

      <SelectDownloadClientModal
        isOpen={selectModalOpen === 'downloadClient'}
        protocol={protocol}
        modalTitle={modalTitle}
        onDownloadClientSelect={onDownloadClientSelect}
        onModalClose={onSelectModalClose}
      />
    </ModalContent>
  );
}

export default OverrideMatchModalContent;
