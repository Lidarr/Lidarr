import React from 'react';
import Modal from 'Components/Modal/Modal';
import ArtistIndexBannerOptionsModalContent from './ArtistIndexBannerOptionsModalContent';

interface ArtistIndexBannerOptionsModalProps {
  isOpen: boolean;
  onModalClose(...args: unknown[]): unknown;
}

function ArtistIndexBannerOptionsModal({
  isOpen,
  onModalClose,
}: ArtistIndexBannerOptionsModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <ArtistIndexBannerOptionsModalContent onModalClose={onModalClose} />
    </Modal>
  );
}

export default ArtistIndexBannerOptionsModal;
