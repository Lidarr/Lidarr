import React from 'react';
import Artist from 'Artist/Artist';
import Modal from 'Components/Modal/Modal';
import SelectArtistModalContent from './SelectArtistModalContent';

interface SelectArtistModalProps {
  isOpen: boolean;
  modalTitle: string;
  onArtistSelect(artist: Artist): void;
  onModalClose(): void;
}

function SelectArtistModal(props: SelectArtistModalProps) {
  const { isOpen, modalTitle, onArtistSelect, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <SelectArtistModalContent
        modalTitle={modalTitle}
        onArtistSelect={onArtistSelect}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default SelectArtistModal;
