import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { SelectProvider } from 'App/SelectContext';
import ArtistAppState, { ArtistIndexAppState } from 'App/State/ArtistAppState';
import ClientSideCollectionAppState from 'App/State/ClientSideCollectionAppState';
import NoArtist from 'Artist/NoArtist';
import { RSS_SYNC } from 'Commands/commandNames';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import withScrollPosition from 'Components/withScrollPosition';
import { align, icons } from 'Helpers/Props';
import SortDirection from 'Helpers/Props/SortDirection';
import {
  setArtistFilter,
  setArtistSort,
  setArtistTableOption,
  setArtistView,
} from 'Store/Actions/artistIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchQueueDetails } from 'Store/Actions/queueActions';
import scrollPositions from 'Store/scrollPositions';
import createArtistClientSideCollectionItemsSelector from 'Store/Selectors/createArtistClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import ArtistIndexFooter from './ArtistIndexFooter';
import ArtistIndexRefreshArtistsButton from './ArtistIndexRefreshArtistsButton';
import ArtistIndexBanners from './Banners/ArtistIndexBanners';
import ArtistIndexBannerOptionsModal from './Banners/Options/ArtistIndexBannerOptionsModal';
import ArtistIndexFilterMenu from './Menus/ArtistIndexFilterMenu';
import ArtistIndexSortMenu from './Menus/ArtistIndexSortMenu';
import ArtistIndexViewMenu from './Menus/ArtistIndexViewMenu';
import ArtistIndexOverviews from './Overview/ArtistIndexOverviews';
import ArtistIndexOverviewOptionsModal from './Overview/Options/ArtistIndexOverviewOptionsModal';
import ArtistIndexPosters from './Posters/ArtistIndexPosters';
import ArtistIndexPosterOptionsModal from './Posters/Options/ArtistIndexPosterOptionsModal';
import ArtistIndexSelectAllButton from './Select/ArtistIndexSelectAllButton';
import ArtistIndexSelectAllMenuItem from './Select/ArtistIndexSelectAllMenuItem';
import ArtistIndexSelectFooter from './Select/ArtistIndexSelectFooter';
import ArtistIndexSelectModeButton from './Select/ArtistIndexSelectModeButton';
import ArtistIndexSelectModeMenuItem from './Select/ArtistIndexSelectModeMenuItem';
import ArtistIndexTable from './Table/ArtistIndexTable';
import ArtistIndexTableOptions from './Table/ArtistIndexTableOptions';
import styles from './ArtistIndex.css';

function getViewComponent(view: string) {
  if (view === 'posters') {
    return ArtistIndexPosters;
  }

  if (view === 'banners') {
    return ArtistIndexBanners;
  }

  if (view === 'overview') {
    return ArtistIndexOverviews;
  }

  return ArtistIndexTable;
}

interface ArtistIndexProps {
  initialScrollTop?: number;
}

const ArtistIndex = withScrollPosition((props: ArtistIndexProps) => {
  const {
    isFetching,
    isPopulated,
    error,
    totalItems,
    items,
    columns,
    selectedFilterKey,
    filters,
    customFilters,
    sortKey,
    sortDirection,
    view,
  }: ArtistAppState & ArtistIndexAppState & ClientSideCollectionAppState =
    useSelector(createArtistClientSideCollectionItemsSelector('artistIndex'));

  const isRssSyncExecuting = useSelector(
    createCommandExecutingSelector(RSS_SYNC)
  );
  const { isSmallScreen } = useSelector(createDimensionsSelector());
  const dispatch = useDispatch();
  const scrollerRef = useRef<HTMLDivElement>(null);
  const [isOptionsModalOpen, setIsOptionsModalOpen] = useState(false);
  const [jumpToCharacter, setJumpToCharacter] = useState<string | undefined>(
    undefined
  );
  const [isSelectMode, setIsSelectMode] = useState(false);

  useEffect(() => {
    dispatch(fetchQueueDetails({ all: true }));
  }, [dispatch]);

  const onRssSyncPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: RSS_SYNC,
      })
    );
  }, [dispatch]);

  const onSelectModePress = useCallback(() => {
    setIsSelectMode(!isSelectMode);
  }, [isSelectMode, setIsSelectMode]);

  const onTableOptionChange = useCallback(
    (payload: unknown) => {
      dispatch(setArtistTableOption(payload));
    },
    [dispatch]
  );

  const onViewSelect = useCallback(
    (value: string) => {
      dispatch(setArtistView({ view: value }));

      if (scrollerRef.current) {
        scrollerRef.current.scrollTo(0, 0);
      }
    },
    [scrollerRef, dispatch]
  );

  const onSortSelect = useCallback(
    (value: string) => {
      dispatch(setArtistSort({ sortKey: value }));
    },
    [dispatch]
  );

  const onFilterSelect = useCallback(
    (value: string) => {
      dispatch(setArtistFilter({ selectedFilterKey: value }));
    },
    [dispatch]
  );

  const onOptionsPress = useCallback(() => {
    setIsOptionsModalOpen(true);
  }, [setIsOptionsModalOpen]);

  const onOptionsModalClose = useCallback(() => {
    setIsOptionsModalOpen(false);
  }, [setIsOptionsModalOpen]);

  const onJumpBarItemPress = useCallback(
    (character: string) => {
      setJumpToCharacter(character);
    },
    [setJumpToCharacter]
  );

  const onScroll = useCallback(
    ({ scrollTop }: { scrollTop: number }) => {
      setJumpToCharacter(undefined);
      scrollPositions.artistIndex = scrollTop;
    },
    [setJumpToCharacter]
  );

  const jumpBarItems = useMemo(() => {
    // Reset if not sorting by sortName
    if (sortKey !== 'sortName') {
      return {
        order: [],
      };
    }

    const characters = items.reduce((acc: Record<string, number>, item) => {
      let char = item.sortName.charAt(0);

      if (!isNaN(Number(char))) {
        char = '#';
      }

      if (char in acc) {
        acc[char] = acc[char] + 1;
      } else {
        acc[char] = 1;
      }

      return acc;
    }, {});

    const order = Object.keys(characters).sort();

    // Reverse if sorting descending
    if (sortDirection === SortDirection.Descending) {
      order.reverse();
    }

    return {
      characters,
      order,
    };
  }, [items, sortKey, sortDirection]);
  const ViewComponent = useMemo(() => getViewComponent(view), [view]);

  const isLoaded = !!(!error && isPopulated && items.length);
  const hasNoArtist = !totalItems;

  return (
    <SelectProvider items={items}>
      <PageContent>
        <PageToolbar>
          <PageToolbarSection>
            <ArtistIndexRefreshArtistsButton
              isSelectMode={isSelectMode}
              selectedFilterKey={selectedFilterKey}
            />

            <PageToolbarButton
              label={translate('RSSSync')}
              iconName={icons.RSS}
              isSpinning={isRssSyncExecuting}
              isDisabled={hasNoArtist}
              onPress={onRssSyncPress}
            />

            <PageToolbarSeparator />

            <ArtistIndexSelectModeButton
              label={isSelectMode ? 'Stop Selecting' : 'Select Artists'}
              iconName={isSelectMode ? icons.ARTIST_ENDED : icons.CHECK}
              isSelectMode={isSelectMode}
              overflowComponent={ArtistIndexSelectModeMenuItem}
              onPress={onSelectModePress}
            />

            <ArtistIndexSelectAllButton
              label="SelectAll"
              isSelectMode={isSelectMode}
              overflowComponent={ArtistIndexSelectAllMenuItem}
            />
          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            {view === 'table' ? (
              <TableOptionsModalWrapper
                columns={columns}
                optionsComponent={ArtistIndexTableOptions}
                onTableOptionChange={onTableOptionChange}
              >
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.TABLE}
                />
              </TableOptionsModalWrapper>
            ) : (
              <PageToolbarButton
                label={translate('Options')}
                iconName={view === 'posters' ? icons.POSTER : icons.OVERVIEW}
                isDisabled={hasNoArtist}
                onPress={onOptionsPress}
              />
            )}

            <PageToolbarSeparator />

            <ArtistIndexViewMenu
              view={view}
              isDisabled={hasNoArtist}
              onViewSelect={onViewSelect}
            />

            <ArtistIndexSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoArtist}
              onSortSelect={onSortSelect}
            />

            <ArtistIndexFilterMenu
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              isDisabled={hasNoArtist}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>
        <div className={styles.pageContentBodyWrapper}>
          <PageContentBody
            ref={scrollerRef}
            className={styles.contentBody}
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            innerClassName={styles[`${view}InnerContentBody`]}
            initialScrollTop={props.initialScrollTop}
            onScroll={onScroll}
          >
            {isFetching && !isPopulated ? <LoadingIndicator /> : null}

            {!isFetching && !!error ? (
              <div className={styles.errorMessage}>
                {getErrorMessage(error, 'Failed to load artist from API')}
              </div>
            ) : null}

            {isLoaded ? (
              <div className={styles.contentBodyContainer}>
                <ViewComponent
                  scrollerRef={scrollerRef}
                  items={items}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  jumpToCharacter={jumpToCharacter}
                  isSelectMode={isSelectMode}
                  isSmallScreen={isSmallScreen}
                />

                <ArtistIndexFooter />
              </div>
            ) : null}

            {!error && isPopulated && !items.length ? (
              <NoArtist totalItems={totalItems} />
            ) : null}
          </PageContentBody>
          {isLoaded && !!jumpBarItems.order.length ? (
            <PageJumpBar
              items={jumpBarItems}
              onItemPress={onJumpBarItemPress}
            />
          ) : null}
        </div>

        {isSelectMode ? <ArtistIndexSelectFooter /> : null}

        {view === 'posters' ? (
          <ArtistIndexPosterOptionsModal
            isOpen={isOptionsModalOpen}
            onModalClose={onOptionsModalClose}
          />
        ) : null}
        {view === 'banners' ? (
          <ArtistIndexBannerOptionsModal
            isOpen={isOptionsModalOpen}
            onModalClose={onOptionsModalClose}
          />
        ) : null}
        {view === 'overview' ? (
          <ArtistIndexOverviewOptionsModal
            isOpen={isOptionsModalOpen}
            onModalClose={onOptionsModalClose}
          />
        ) : null}
      </PageContent>
    </SelectProvider>
  );
}, 'artistIndex');

export default ArtistIndex;
