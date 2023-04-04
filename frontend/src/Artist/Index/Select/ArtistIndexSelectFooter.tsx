import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { useSelect } from 'App/SelectContext';
import AppState from 'App/State/AppState';
import { RENAME_ARTIST, RETAG_ARTIST } from 'Commands/commandNames';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import usePrevious from 'Helpers/Hooks/usePrevious';
import { kinds } from 'Helpers/Props';
import {
  saveArtistEditor,
  updateArtistsMonitor,
} from 'Store/Actions/artistActions';
import { fetchRootFolders } from 'Store/Actions/settingsActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import ChangeMonitoringModal from './AlbumStudio/ChangeMonitoringModal';
import RetagArtistModal from './AudioTags/RetagArtistModal';
import DeleteArtistModal from './Delete/DeleteArtistModal';
import EditArtistModal from './Edit/EditArtistModal';
import OrganizeArtistModal from './Organize/OrganizeArtistModal';
import TagsModal from './Tags/TagsModal';
import styles from './ArtistIndexSelectFooter.css';

interface SavePayload {
  monitored?: boolean;
  qualityProfileId?: number;
  metadataProfileId?: number;
  rootFolderPath?: string;
  moveFiles?: boolean;
}

const artistEditorSelector = createSelector(
  (state: AppState) => state.artist,
  (artist) => {
    const { isSaving, isDeleting, deleteError } = artist;

    return {
      isSaving,
      isDeleting,
      deleteError,
    };
  }
);

function ArtistIndexSelectFooter() {
  const { isSaving, isDeleting, deleteError } =
    useSelector(artistEditorSelector);

  const isOrganizingArtist = useSelector(
    createCommandExecutingSelector(RENAME_ARTIST)
  );
  const isRetaggingArtist = useSelector(
    createCommandExecutingSelector(RETAG_ARTIST)
  );

  const dispatch = useDispatch();

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isOrganizeModalOpen, setIsOrganizeModalOpen] = useState(false);
  const [isRetaggingModalOpen, setIsRetaggingModalOpen] = useState(false);
  const [isTagsModalOpen, setIsTagsModalOpen] = useState(false);
  const [isMonitoringModalOpen, setIsMonitoringModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isSavingArtist, setIsSavingArtist] = useState(false);
  const [isSavingTags, setIsSavingTags] = useState(false);
  const [isSavingMonitoring, setIsSavingMonitoring] = useState(false);
  const previousIsDeleting = usePrevious(isDeleting);

  const [selectState, selectDispatch] = useSelect();
  const { selectedState } = selectState;

  const artistIds = useMemo(() => {
    return getSelectedIds(selectedState);
  }, [selectedState]);

  const selectedCount = artistIds.length;

  const onEditPress = useCallback(() => {
    setIsEditModalOpen(true);
  }, [setIsEditModalOpen]);

  const onEditModalClose = useCallback(() => {
    setIsEditModalOpen(false);
  }, [setIsEditModalOpen]);

  const onSavePress = useCallback(
    (payload: SavePayload) => {
      setIsSavingArtist(true);
      setIsEditModalOpen(false);

      dispatch(
        saveArtistEditor({
          ...payload,
          artistIds,
        })
      );
    },
    [artistIds, dispatch]
  );

  const onOrganizePress = useCallback(() => {
    setIsOrganizeModalOpen(true);
  }, [setIsOrganizeModalOpen]);

  const onOrganizeModalClose = useCallback(() => {
    setIsOrganizeModalOpen(false);
  }, [setIsOrganizeModalOpen]);

  const onRetagPress = useCallback(() => {
    setIsRetaggingModalOpen(true);
  }, [setIsRetaggingModalOpen]);

  const onRetagModalClose = useCallback(() => {
    setIsRetaggingModalOpen(false);
  }, [setIsRetaggingModalOpen]);

  const onTagsPress = useCallback(() => {
    setIsTagsModalOpen(true);
  }, [setIsTagsModalOpen]);

  const onTagsModalClose = useCallback(() => {
    setIsTagsModalOpen(false);
  }, [setIsTagsModalOpen]);

  const onApplyTagsPress = useCallback(
    (tags: number[], applyTags: string) => {
      setIsSavingTags(true);
      setIsTagsModalOpen(false);

      dispatch(
        saveArtistEditor({
          artistIds,
          tags,
          applyTags,
        })
      );
    },
    [artistIds, dispatch]
  );

  const onMonitoringPress = useCallback(() => {
    setIsMonitoringModalOpen(true);
  }, [setIsMonitoringModalOpen]);

  const onMonitoringClose = useCallback(() => {
    setIsMonitoringModalOpen(false);
  }, [setIsMonitoringModalOpen]);

  const onMonitoringSavePress = useCallback(
    (monitor: string) => {
      setIsSavingMonitoring(true);
      setIsMonitoringModalOpen(false);

      dispatch(
        updateArtistsMonitor({
          artistIds,
          monitor,
        })
      );
    },
    [artistIds, dispatch]
  );

  const onDeletePress = useCallback(() => {
    setIsDeleteModalOpen(true);
  }, [setIsDeleteModalOpen]);

  const onDeleteModalClose = useCallback(() => {
    setIsDeleteModalOpen(false);
  }, []);

  useEffect(() => {
    if (!isSaving) {
      setIsSavingArtist(false);
      setIsSavingTags(false);
      setIsSavingMonitoring(false);
    }
  }, [isSaving]);

  useEffect(() => {
    if (previousIsDeleting && !isDeleting && !deleteError) {
      selectDispatch({ type: 'unselectAll' });
    }
  }, [previousIsDeleting, isDeleting, deleteError, selectDispatch]);

  useEffect(() => {
    dispatch(fetchRootFolders());
  }, [dispatch]);

  const anySelected = selectedCount > 0;

  return (
    <PageContentFooter className={styles.footer}>
      <div className={styles.buttons}>
        <div className={styles.actionButtons}>
          <SpinnerButton
            isSpinning={isSaving && isSavingArtist}
            isDisabled={!anySelected || isOrganizingArtist || isRetaggingArtist}
            onPress={onEditPress}
          >
            {translate('Edit')}
          </SpinnerButton>

          <SpinnerButton
            kind={kinds.WARNING}
            isSpinning={isOrganizingArtist}
            isDisabled={!anySelected || isOrganizingArtist || isRetaggingArtist}
            onPress={onOrganizePress}
          >
            {translate('RenameFiles')}
          </SpinnerButton>

          <SpinnerButton
            kind={kinds.WARNING}
            isSpinning={isRetaggingArtist}
            isDisabled={!anySelected || isOrganizingArtist || isRetaggingArtist}
            onPress={onRetagPress}
          >
            {translate('WriteMetadataTags')}
          </SpinnerButton>

          <SpinnerButton
            isSpinning={isSaving && isSavingTags}
            isDisabled={!anySelected || isOrganizingArtist}
            onPress={onTagsPress}
          >
            {translate('SetAppTags')}
          </SpinnerButton>

          <SpinnerButton
            isSpinning={isSaving && isSavingMonitoring}
            isDisabled={!anySelected || isOrganizingArtist || isRetaggingArtist}
            onPress={onMonitoringPress}
          >
            {translate('UpdateMonitoring')}
          </SpinnerButton>
        </div>

        <div className={styles.deleteButtons}>
          <SpinnerButton
            kind={kinds.DANGER}
            isSpinning={isDeleting}
            isDisabled={!anySelected || isDeleting}
            onPress={onDeletePress}
          >
            {translate('Delete')}
          </SpinnerButton>
        </div>
      </div>

      <div className={styles.selected}>
        {translate('CountArtistsSelected', { count: selectedCount })}
      </div>

      <EditArtistModal
        isOpen={isEditModalOpen}
        artistIds={artistIds}
        onSavePress={onSavePress}
        onModalClose={onEditModalClose}
      />

      <TagsModal
        isOpen={isTagsModalOpen}
        artistIds={artistIds}
        onApplyTagsPress={onApplyTagsPress}
        onModalClose={onTagsModalClose}
      />

      <ChangeMonitoringModal
        isOpen={isMonitoringModalOpen}
        artistIds={artistIds}
        onSavePress={onMonitoringSavePress}
        onModalClose={onMonitoringClose}
      />

      <OrganizeArtistModal
        isOpen={isOrganizeModalOpen}
        artistIds={artistIds}
        onModalClose={onOrganizeModalClose}
      />

      <RetagArtistModal
        isOpen={isRetaggingModalOpen}
        artistIds={artistIds}
        onModalClose={onRetagModalClose}
      />

      <DeleteArtistModal
        isOpen={isDeleteModalOpen}
        artistIds={artistIds}
        onModalClose={onDeleteModalClose}
      />
    </PageContentFooter>
  );
}

export default ArtistIndexSelectFooter;
