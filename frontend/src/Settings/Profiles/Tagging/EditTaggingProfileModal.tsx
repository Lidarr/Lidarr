import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditTaggingProfileModalContent from './EditTaggingProfileModalContent';

interface EditTaggingProfileModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
  onDeleteTaggingProfilePress?: () => void;
}

function EditTaggingProfileModal(props: EditTaggingProfileModalProps) {
  const { id, isOpen, onModalClose, onDeleteTaggingProfilePress } = props;

  const dispatch = useDispatch();

  const onModalCloseWrapper = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'settings.taggingProfiles' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal
      size={sizes.MEDIUM}
      isOpen={isOpen}
      onModalClose={onModalCloseWrapper}
    >
      <EditTaggingProfileModalContent
        id={id}
        onModalClose={onModalCloseWrapper}
        onDeleteTaggingProfilePress={onDeleteTaggingProfilePress}
      />
    </Modal>
  );
}

export default EditTaggingProfileModal;
