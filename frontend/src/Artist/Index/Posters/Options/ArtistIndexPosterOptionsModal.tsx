import React from 'react';
import Modal from 'Components/Modal/Modal';
import ArtistIndexPosterOptionsModalContent from './ArtistIndexPosterOptionsModalContent';

interface ArtistIndexPosterOptionsModalProps {
  isOpen: boolean;
  onModalClose(...args: unknown[]): unknown;
}

function ArtistIndexPosterOptionsModal({
  isOpen,
  onModalClose,
}: ArtistIndexPosterOptionsModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <ArtistIndexPosterOptionsModalContent onModalClose={onModalClose} />
    </Modal>
  );
}

export default ArtistIndexPosterOptionsModal;
