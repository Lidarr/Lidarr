import React from 'react';
import Modal from 'Components/Modal/Modal';
import SelectAlbumModalContent, {
  SelectedAlbum,
} from './SelectAlbumModalContent';

interface SelectAlbumModalProps {
  isOpen: boolean;
  selectedIds: number[] | string[];
  artistId?: number;
  selectedDetails?: string;
  modalTitle: string;
  onAlbumsSelect(selectedAlbums: SelectedAlbum[]): void;
  onModalClose(): void;
}

function SelectAlbumModal(props: SelectAlbumModalProps) {
  const {
    isOpen,
    selectedIds,
    artistId,
    selectedDetails,
    modalTitle,
    onAlbumsSelect,
    onModalClose,
  } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <SelectAlbumModalContent
        selectedIds={selectedIds}
        artistId={artistId}
        selectedDetails={selectedDetails}
        modalTitle={modalTitle}
        onAlbumsSelect={onAlbumsSelect}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default SelectAlbumModal;
