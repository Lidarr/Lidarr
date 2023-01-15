import React from 'react';
import Modal from 'Components/Modal/Modal';
import EditArtistModalContent from './EditArtistModalContent';

interface EditArtistModalProps {
  isOpen: boolean;
  artistIds: number[];
  onSavePress(payload: object): void;
  onModalClose(): void;
}

function EditArtistModal(props: EditArtistModalProps) {
  const { isOpen, artistIds, onSavePress, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <EditArtistModalContent
        artistIds={artistIds}
        onSavePress={onSavePress}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default EditArtistModal;
