import { throttle } from 'lodash';
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useSelector } from 'react-redux';
import { FixedSizeList as List, ListChildComponentProps } from 'react-window';
import Artist from 'Artist/Artist';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import Column from 'Components/Table/Column';
import VirtualTableRowButton from 'Components/Table/VirtualTableRowButton';
import { scrollDirections } from 'Helpers/Props';
import createAllArtistSelector from 'Store/Selectors/createAllArtistSelector';
import dimensions from 'Styles/Variables/dimensions';
import translate from 'Utilities/String/translate';
import SelectArtistModalTableHeader from './SelectArtistModalTableHeader';
import SelectArtistRow from './SelectArtistRow';
import styles from './SelectArtistModalContent.css';

const columns = [
  {
    name: 'artistName',
    label: () => translate('Artist'),
    isVisible: true,
  },
  {
    name: 'foreignArtistId',
    label: () => translate('MusicbrainzId'),
    isVisible: true,
  },
];

const bodyPadding = parseInt(dimensions.pageContentBodyPadding);

interface SelectArtistModalContentProps {
  modalTitle: string;
  onArtistSelect(artist: Artist): void;
  onModalClose(): void;
}

interface RowItemData {
  items: Artist[];
  columns: Column[];
  onArtistSelect(artistId: number): void;
}

const Row: React.FC<ListChildComponentProps<RowItemData>> = ({
  index,
  style,
  data,
}) => {
  const { items, columns, onArtistSelect } = data;

  if (index >= items.length) {
    return null;
  }

  const artist = items[index];

  return (
    <VirtualTableRowButton
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        ...style,
      }}
      onPress={() => onArtistSelect(artist.id)}
    >
      <SelectArtistRow
        key={artist.id}
        id={artist.id}
        artistName={artist.artistName}
        foreignArtistId={artist.foreignArtistId}
        columns={columns}
        onArtistSelect={onArtistSelect}
      />
    </VirtualTableRowButton>
  );
};

function SelectArtistModalContent(props: SelectArtistModalContentProps) {
  const { modalTitle, onArtistSelect, onModalClose } = props;

  const listRef = useRef<List<RowItemData>>(null);
  const scrollerRef = useRef<HTMLDivElement>(null);
  const allArtist: Artist[] = useSelector(createAllArtistSelector());
  const [filter, setFilter] = useState('');
  const [size, setSize] = useState({ width: 0, height: 0 });
  const windowHeight = window.innerHeight;

  useEffect(() => {
    const current = scrollerRef?.current as HTMLElement;

    if (current) {
      const width = current.clientWidth;
      const height = current.clientHeight;
      const padding = bodyPadding - 5;

      setSize({
        width: width - padding * 2,
        height: height + padding,
      });
    }
  }, [windowHeight, scrollerRef]);

  useEffect(() => {
    const currentScrollerRef = scrollerRef.current as HTMLElement;
    const currentScrollListener = currentScrollerRef;

    const handleScroll = throttle(() => {
      const { offsetTop = 0 } = currentScrollerRef;
      const scrollTop = currentScrollerRef.scrollTop - offsetTop;

      listRef.current?.scrollTo(scrollTop);
    }, 10);

    currentScrollListener.addEventListener('scroll', handleScroll);

    return () => {
      handleScroll.cancel();

      if (currentScrollListener) {
        currentScrollListener.removeEventListener('scroll', handleScroll);
      }
    };
  }, [listRef, scrollerRef]);

  const onFilterChange = useCallback(
    ({ value }: { value: string }) => {
      setFilter(value);
    },
    [setFilter]
  );

  const onArtistSelectWrapper = useCallback(
    (artistId: number) => {
      const artist = allArtist.find((s) => s.id === artistId) as Artist;

      onArtistSelect(artist);
    },
    [allArtist, onArtistSelect]
  );

  const items = useMemo(() => {
    const sorted = [...allArtist].sort((a, b) =>
      a.sortName.localeCompare(b.sortName)
    );

    return sorted.filter(
      (item) =>
        item.artistName.toLowerCase().includes(filter.toLowerCase()) ||
        item.foreignArtistId.toLowerCase().includes(filter.toLowerCase())
    );
  }, [allArtist, filter]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{modalTitle} - Select Artist</ModalHeader>

      <ModalBody
        className={styles.modalBody}
        scrollDirection={scrollDirections.NONE}
      >
        <TextInput
          className={styles.filterInput}
          placeholder={translate('FilterArtistPlaceholder')}
          name="filter"
          value={filter}
          autoFocus={true}
          onChange={onFilterChange}
        />

        <Scroller
          className={styles.scroller}
          autoFocus={false}
          ref={scrollerRef}
        >
          <SelectArtistModalTableHeader columns={columns} />
          <List<RowItemData>
            ref={listRef}
            style={{
              width: '100%',
              height: '100%',
              overflow: 'none',
            }}
            width={size.width}
            height={size.height}
            itemCount={items.length}
            itemSize={38}
            itemData={{
              items,
              columns,
              onArtistSelect: onArtistSelectWrapper,
            }}
          >
            {Row}
          </List>
        </Scroller>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default SelectArtistModalContent;
