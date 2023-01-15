import React from 'react';
import Modal from 'Components/Modal/Modal';
import DeleteArtistModalContent from './DeleteArtistModalContent';

interface DeleteArtistModalProps {
  isOpen: boolean;
  artistIds: number[];
  onModalClose(): void;
}

function DeleteArtistModal(props: DeleteArtistModalProps) {
  const { isOpen, artistIds, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <DeleteArtistModalContent
        artistIds={artistIds}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default DeleteArtistModal;
