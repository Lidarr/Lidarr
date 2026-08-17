import React, { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import usePrevious from 'Helpers/Hooks/usePrevious';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import {
  fetchTaggingProfileSchema,
  saveTaggingProfile,
  setTaggingProfileValue,
} from 'Store/Actions/settingsActions';
import createProviderSettingsSelector from 'Store/Selectors/createProviderSettingsSelector';
import { PendingSection } from 'typings/pending';
import TaggingProfile from 'typings/TaggingProfile';
import translate from 'Utilities/String/translate';
import styles from './EditTaggingProfileModalContent.css';

const writeAudioTagOptions = [
  { key: 'sync', value: 'All files; keep in sync with MusicBrainz' },
  { key: 'allFiles', value: 'All files; initial import only' },
  { key: 'newFiles', value: 'For new downloads only' },
  { key: 'no', value: 'Never' },
];

interface EditTaggingProfileModalContentProps {
  id?: number;
  onModalClose: () => void;
  onDeleteTaggingProfilePress?: () => void;
}

function EditTaggingProfileModalContent(
  props: EditTaggingProfileModalContentProps
) {
  const { id, onModalClose, onDeleteTaggingProfilePress } = props;

  const dispatch = useDispatch();

  const providerSettingsSelector = useMemo(
    () => createProviderSettingsSelector('taggingProfiles'),
    []
  );

  const {
    item,
    isFetching,
    isPopulated,
    error,
    isSaving,
    saveError,
    validationErrors,
    validationWarnings,
  } = useSelector((state: AppState) => providerSettingsSelector(state, { id }));

  const profile = item as PendingSection<TaggingProfile>;
  const wasSaving = usePrevious(isSaving);

  useEffect(() => {
    if (!id && !isPopulated) {
      dispatch(fetchTaggingProfileSchema());
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (wasSaving && !isSaving && !saveError) {
      onModalClose();
    }
  }, [wasSaving, isSaving, saveError, onModalClose]);

  const onInputChange = useCallback(
    ({ name, value }: { name: string; value: unknown }) => {
      // @ts-expect-error - actions are not typed
      dispatch(setTaggingProfileValue({ name, value }));
    },
    [dispatch]
  );

  const onSavePress = useCallback(() => {
    dispatch(saveTaggingProfile({ id }));
  }, [dispatch, id]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? translate('EditTaggingProfile') : translate('AddTaggingProfile')}
      </ModalHeader>

      <ModalBody>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && !!error ? (
          <Alert kind={kinds.DANGER}>
            {translate('AddTaggingProfileError')}
          </Alert>
        ) : null}

        {!isFetching && isPopulated && !error ? (
          <Form
            validationErrors={validationErrors}
            validationWarnings={validationWarnings}
          >
            <FormGroup size={sizes.SMALL}>
              <FormLabel size={sizes.SMALL}>{translate('Name')}</FormLabel>

              <FormInputGroup
                type={inputTypes.TEXT}
                name="name"
                {...profile.name}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('TagAudioFilesWithMetadata')}</FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="writeAudioTags"
                helpTextWarning={translate('WriteAudioTagsHelpTextWarning')}
                helpLink="https://wiki.servarr.com/lidarr/settings#write-metadata-to-audio-files"
                values={writeAudioTagOptions}
                {...profile.writeAudioTags}
                onChange={onInputChange}
              />
            </FormGroup>

            {profile.writeAudioTags.value === 'no' ? null : (
              <FormGroup>
                <FormLabel>{translate('EmbedCoverArtInAudioFiles')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="embedCoverArt"
                  helpText={translate('EmbedCoverArtHelpText')}
                  {...profile.embedCoverArt}
                  onChange={onInputChange}
                />
              </FormGroup>
            )}

            <FormGroup>
              <FormLabel>{translate('ScrubExistingTags')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="scrubAudioTags"
                helpText={translate('ScrubAudioTagsHelpText')}
                {...profile.scrubAudioTags}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('DownloadClients')}</FormLabel>

              <FormInputGroup
                type={inputTypes.DOWNLOAD_CLIENT_SELECT}
                name="downloadClientIds"
                helpText={translate('TaggingProfileDownloadClientsHelpText')}
                {...profile.downloadClientIds}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('SkipHardlinkedFiles')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="skipHardlinkedFiles"
                helpText={translate(
                  'TaggingProfileSkipHardlinkedFilesHelpText'
                )}
                {...profile.skipHardlinkedFiles}
                onChange={onInputChange}
              />
            </FormGroup>

            {id === 1 ? (
              <Alert>{translate('DefaultTaggingProfileMessage')}</Alert>
            ) : (
              <FormGroup size={sizes.SMALL}>
                <FormLabel size={sizes.SMALL}>{translate('Tags')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.TAG}
                  name="tags"
                  helpText={translate('TaggingProfileTagsHelpText')}
                  {...profile.tags}
                  onChange={onInputChange}
                />
              </FormGroup>
            )}
          </Form>
        ) : null}
      </ModalBody>
      <ModalFooter>
        {id && id > 1 ? (
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeleteTaggingProfilePress}
          >
            {translate('Delete')}
          </Button>
        ) : null}

        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError}
          onPress={onSavePress}
        >
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default EditTaggingProfileModalContent;
