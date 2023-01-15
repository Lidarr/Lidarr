import React from 'react';
import Modal from 'Components/Modal/Modal';
import OrganizeArtistModalContent from './OrganizeArtistModalContent';

interface OrganizeArtistModalProps {
  isOpen: boolean;
  artistIds: number[];
  onModalClose: () => void;
}

function OrganizeArtistModal(props: OrganizeArtistModalProps) {
  const { isOpen, onModalClose, ...otherProps } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <OrganizeArtistModalContent {...otherProps} onModalClose={onModalClose} />
    </Modal>
  );
}

export default OrganizeArtistModal;
