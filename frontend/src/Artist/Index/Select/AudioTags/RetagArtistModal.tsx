import React from 'react';
import Modal from 'Components/Modal/Modal';
import RetagArtistModalContent from './RetagArtistModalContent';

interface RetagArtistModalProps {
  isOpen: boolean;
  artistIds: number[];
  onModalClose: () => void;
}

function RetagArtistModal(props: RetagArtistModalProps) {
  const { isOpen, onModalClose, ...otherProps } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <RetagArtistModalContent {...otherProps} onModalClose={onModalClose} />
    </Modal>
  );
}

export default RetagArtistModal;
