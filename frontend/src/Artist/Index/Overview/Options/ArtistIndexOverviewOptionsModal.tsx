import React from 'react';
import Modal from 'Components/Modal/Modal';
import ArtistIndexOverviewOptionsModalContent from './ArtistIndexOverviewOptionsModalContent';

interface ArtistIndexOverviewOptionsModalProps {
  isOpen: boolean;
  onModalClose(...args: unknown[]): void;
}

function ArtistIndexOverviewOptionsModal({
  isOpen,
  onModalClose,
  ...otherProps
}: ArtistIndexOverviewOptionsModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <ArtistIndexOverviewOptionsModalContent
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default ArtistIndexOverviewOptionsModal;
