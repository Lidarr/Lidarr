import { orderBy } from 'lodash';
import React, { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Artist from 'Artist/Artist';
import { RETAG_ARTIST } from 'Commands/commandNames';
import Alert from 'Components/Alert';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, kinds } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createAllArtistSelector from 'Store/Selectors/createAllArtistSelector';
import translate from 'Utilities/String/translate';
import styles from './RetagArtistModalContent.css';

interface RetagArtistModalContentProps {
  artistIds: number[];
  onModalClose: () => void;
}

function RetagArtistModalContent(props: RetagArtistModalContentProps) {
  const { artistIds, onModalClose } = props;

  const allArtists: Artist[] = useSelector(createAllArtistSelector());
  const dispatch = useDispatch();

  const artistNames = useMemo(() => {
    const artists = artistIds.reduce((acc: Artist[], id) => {
      const a = allArtists.find((a) => a.id === id);

      if (a) {
        acc.push(a);
      }

      return acc;
    }, []);

    const sorted = orderBy(artists, ['sortName']);

    return sorted.map((a) => a.artistName);
  }, [artistIds, allArtists]);

  const onRetagPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: RETAG_ARTIST,
        artistIds,
      })
    );

    onModalClose();
  }, [artistIds, onModalClose, dispatch]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('RetagSelectedArtists')}</ModalHeader>

      <ModalBody>
        <Alert>
          Tip: To preview the tags that will be written, select "Cancel", then
          select any artist name and use the
          <Icon className={styles.retagIcon} name={icons.RETAG} />
        </Alert>

        <div className={styles.message}>
          Are you sure you want to retag all files in the {artistNames.length}{' '}
          selected artist?
        </div>

        <ul>
          {artistNames.map((artistName) => {
            return <li key={artistName}>{artistName}</li>;
          })}
        </ul>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.DANGER} onPress={onRetagPress}>
          {translate('Retag')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default RetagArtistModalContent;
