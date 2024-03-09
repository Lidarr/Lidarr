import React, { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import Album from 'Album/Album';
import AlbumAppState from 'App/State/AlbumAppState';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import useSelectState from 'Helpers/Hooks/useSelectState';
import { kinds, scrollDirections } from 'Helpers/Props';
import SortDirection from 'Helpers/Props/SortDirection';
import {
  clearAlbums,
  fetchAlbums,
  setAlbumsSort,
} from 'Store/Actions/albumSelectionActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import { CheckInputChanged } from 'typings/inputs';
import { SelectStateInputProps } from 'typings/props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import SelectAlbumRow from './SelectAlbumRow';
import styles from './SelectAlbumModalContent.css';

const columns = [
  {
    name: 'title',
    label: () => translate('AlbumTitle'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'albumType',
    label: () => translate('AlbumType'),
    isVisible: true,
  },
  {
    name: 'releaseDate',
    label: () => translate('ReleaseDate'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'status',
    label: () => translate('AlbumStatus'),
    isVisible: true,
  },
  {
    name: 'foreignAlbumId',
    label: () => translate('MusicbrainzId'),
    isVisible: true,
  },
];

function albumsSelector() {
  return createSelector(
    createClientSideCollectionSelector('albumSelection'),
    (albums: AlbumAppState) => {
      return albums;
    }
  );
}

export interface SelectedAlbum {
  id: number;
  albums: Album[];
}

interface SelectAlbumModalContentProps {
  selectedIds: number[] | string[];
  artistId?: number;
  selectedDetails?: string;
  modalTitle: string;
  onAlbumsSelect(selectedAlbums: SelectedAlbum[]): unknown;
  onModalClose(): unknown;
}

//
// Render

function SelectAlbumModalContent(props: SelectAlbumModalContentProps) {
  const {
    selectedIds,
    artistId,
    selectedDetails,
    modalTitle,
    onAlbumsSelect,
    onModalClose,
  } = props;

  const [filter, setFilter] = useState('');
  const [selectState, setSelectState] = useSelectState();

  const { allSelected, allUnselected, selectedState } = selectState;
  const { isFetching, isPopulated, items, error, sortKey, sortDirection } =
    useSelector(albumsSelector());
  const dispatch = useDispatch();

  const errorMessage = getErrorMessage(error, translate('AlbumsLoadError'));
  const selectedCount = selectedIds.length;
  const selectedAlbumsCount = getSelectedIds(selectedState).length;
  const selectionIsValid =
    selectedAlbumsCount > 0 && selectedAlbumsCount % selectedCount === 0;

  const onFilterChange = useCallback(
    ({ value }: { value: string }) => {
      setFilter(value.toLowerCase());
    },
    [setFilter]
  );

  const onSelectAllChange = useCallback(
    ({ value }: CheckInputChanged) => {
      setSelectState({ type: value ? 'selectAll' : 'unselectAll', items });
    },
    [items, setSelectState]
  );

  const onSelectedChange = useCallback(
    ({ id, value, shiftKey = false }: SelectStateInputProps) => {
      setSelectState({
        type: 'toggleSelected',
        items,
        id,
        isSelected: value,
        shiftKey,
      });
    },
    [items, setSelectState]
  );

  const onSortPress = useCallback(
    (newSortKey: string, newSortDirection: SortDirection) => {
      dispatch(
        setAlbumsSort({
          sortKey: newSortKey,
          sortDirection: newSortDirection,
        })
      );
    },
    [dispatch]
  );

  const onAlbumsSelectWrapper = useCallback(() => {
    const albumIds: number[] = getSelectedIds(selectedState);

    const selectedAlbums = items.reduce((acc: Album[], item) => {
      if (albumIds.indexOf(item.id) > -1) {
        acc.push(item);
      }

      return acc;
    }, []);

    const albumsPerFile = selectedAlbums.length / selectedIds.length;
    const sortedAlbums = selectedAlbums.sort((a, b) =>
      a.title.localeCompare(b.title)
    );

    const mappedAlbums = selectedIds.map((id, index): SelectedAlbum => {
      const startingIndex = index * albumsPerFile;
      const albums = sortedAlbums.slice(
        startingIndex,
        startingIndex + albumsPerFile
      );

      return {
        id: id as number,
        albums,
      };
    });

    onAlbumsSelect(mappedAlbums);
  }, [selectedIds, items, selectedState, onAlbumsSelect]);

  useEffect(
    () => {
      dispatch(fetchAlbums({ artistId }));

      return () => {
        dispatch(clearAlbums());
      };
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    []
  );

  let details = selectedDetails;

  if (!details) {
    details =
      selectedCount > 1
        ? translate('CountSelectedFiles', { selectedCount })
        : translate('CountSelectedFile', { selectedCount });
  }

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('SelectAlbumsModalTitle', { modalTitle })}
      </ModalHeader>

      <ModalBody
        className={styles.modalBody}
        scrollDirection={scrollDirections.NONE}
      >
        <TextInput
          className={styles.filterInput}
          placeholder={translate('FilterAlbumsPlaceholder')}
          name="filter"
          value={filter}
          autoFocus={true}
          onChange={onFilterChange}
        />

        <Scroller className={styles.scroller} autoFocus={false}>
          {isFetching ? <LoadingIndicator /> : null}

          {error ? <div>{errorMessage}</div> : null}

          {isPopulated && !!items.length ? (
            <Table
              columns={columns}
              selectAll={true}
              allSelected={allSelected}
              allUnselected={allUnselected}
              sortKey={sortKey}
              sortDirection={sortDirection}
              onSortPress={onSortPress}
              onSelectAllChange={onSelectAllChange}
            >
              <TableBody>
                {items.map((item) => {
                  return item.title.toLowerCase().includes(filter) ||
                    item.foreignAlbumId.toLowerCase().includes(filter) ? (
                    <SelectAlbumRow
                      key={item.id}
                      id={item.id}
                      foreignAlbumId={item.foreignAlbumId}
                      title={item.title}
                      disambiguation={item.disambiguation}
                      albumType={item.albumType}
                      releaseDate={item.releaseDate}
                      statistics={item.statistics}
                      monitored={item.monitored}
                      isSelected={selectedState[item.id]}
                      onSelectedChange={onSelectedChange}
                    />
                  ) : null;
                })}
              </TableBody>
            </Table>
          ) : null}

          {isPopulated && !items.length
            ? translate('NoAlbumsFoundForSelectedArtist')
            : null}
        </Scroller>
      </ModalBody>

      <ModalFooter className={styles.footer}>
        <div className={styles.details}>{details}</div>

        <div className={styles.buttons}>
          <Button onPress={onModalClose}>{translate('Cancel')}</Button>

          <Button
            kind={kinds.SUCCESS}
            isDisabled={!selectionIsValid}
            onPress={onAlbumsSelectWrapper}
          >
            {translate('SelectAlbums')}
          </Button>
        </div>
      </ModalFooter>
    </ModalContent>
  );
}

export default SelectAlbumModalContent;
