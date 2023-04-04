import { orderBy } from 'lodash';
import React, { useCallback, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Artist from 'Artist/Artist';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import { bulkDeleteArtist, setDeleteOption } from 'Store/Actions/artistActions';
import createAllArtistSelector from 'Store/Selectors/createAllArtistSelector';
import { CheckInputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import styles from './DeleteArtistModalContent.css';

interface DeleteArtistModalContentProps {
  artistIds: number[];
  onModalClose(): void;
}

const selectDeleteOptions = createSelector(
  (state: AppState) => state.artist.deleteOptions,
  (deleteOptions) => deleteOptions
);

function DeleteArtistModalContent(props: DeleteArtistModalContentProps) {
  const { artistIds, onModalClose } = props;

  const { addImportListExclusion } = useSelector(selectDeleteOptions);
  const allArtists: Artist[] = useSelector(createAllArtistSelector());
  const dispatch = useDispatch();

  const [deleteFiles, setDeleteFiles] = useState(false);

  const artists = useMemo((): Artist[] => {
    const artistList = artistIds.map((id) => {
      return allArtists.find((a) => a.id === id);
    }) as Artist[];

    return orderBy(artistList, ['sortName']);
  }, [artistIds, allArtists]);

  const onDeleteFilesChange = useCallback(
    ({ value }: CheckInputChanged) => {
      setDeleteFiles(value);
    },
    [setDeleteFiles]
  );

  const onDeleteOptionChange = useCallback(
    ({ name, value }: { name: string; value: boolean }) => {
      dispatch(
        setDeleteOption({
          [name]: value,
        })
      );
    },
    [dispatch]
  );

  const onDeleteArtistConfirmed = useCallback(() => {
    setDeleteFiles(false);

    dispatch(
      bulkDeleteArtist({
        artistIds,
        deleteFiles,
        addImportListExclusion,
      })
    );

    onModalClose();
  }, [
    artistIds,
    deleteFiles,
    addImportListExclusion,
    setDeleteFiles,
    dispatch,
    onModalClose,
  ]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('DeleteSelectedArtists')}</ModalHeader>

      <ModalBody>
        <div>
          <FormGroup>
            <FormLabel>{translate('AddListExclusion')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="addImportListExclusion"
              value={addImportListExclusion}
              helpText={translate('AddListExclusionHelpText')}
              onChange={onDeleteOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {artists.length > 1
                ? translate('DeleteArtistFolders')
                : translate('DeleteArtistFolder')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="deleteFiles"
              value={deleteFiles}
              helpText={
                artists.length > 1
                  ? translate('DeleteArtistFoldersHelpText')
                  : translate('DeleteArtistFolderHelpText')
              }
              kind={kinds.DANGER}
              onChange={onDeleteFilesChange}
            />
          </FormGroup>
        </div>

        <div className={styles.message}>
          {deleteFiles
            ? translate('DeleteArtistFolderCountWithFilesConfirmation', {
                count: artists.length,
              })
            : translate('DeleteArtistFolderCountConfirmation', {
                count: artists.length,
              })}
        </div>

        <ul>
          {artists.map((a) => {
            return (
              <li key={a.artistName}>
                <span>{a.artistName}</span>

                {deleteFiles && (
                  <span className={styles.pathContainer}>
                    -<span className={styles.path}>{a.path}</span>
                  </span>
                )}
              </li>
            );
          })}
        </ul>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.DANGER} onPress={onDeleteArtistConfirmed}>
          {translate('Delete')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default DeleteArtistModalContent;
