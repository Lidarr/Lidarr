import { throttle } from 'lodash';
import React, { RefObject, useEffect, useMemo, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { FixedSizeGrid as Grid, GridChildComponentProps } from 'react-window';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Artist from 'Artist/Artist';
import ArtistIndexBanner from 'Artist/Index/Banners/ArtistIndexBanner';
import useMeasure from 'Helpers/Hooks/useMeasure';
import SortDirection from 'Helpers/Props/SortDirection';
import dimensions from 'Styles/Variables/dimensions';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';

const bodyPadding = parseInt(dimensions.pageContentBodyPadding);
const bodyPaddingSmallScreen = parseInt(
  dimensions.pageContentBodyPaddingSmallScreen
);
const columnPadding = parseInt(dimensions.artistIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.artistIndexColumnPaddingSmallScreen
);
const progressBarHeight = parseInt(dimensions.progressBarSmallHeight);
const detailedProgressBarHeight = parseInt(dimensions.progressBarMediumHeight);

const ADDITIONAL_COLUMN_COUNT: Record<string, number> = {
  small: 3,
  medium: 2,
  large: 1,
};

interface CellItemData {
  layout: {
    columnCount: number;
    padding: number;
    bannerWidth: number;
    bannerHeight: number;
  };
  items: Artist[];
  sortKey: string;
  isSelectMode: boolean;
}

interface ArtistIndexBannersProps {
  items: Artist[];
  sortKey: string;
  sortDirection?: SortDirection;
  jumpToCharacter?: string;
  scrollTop?: number;
  scrollerRef: RefObject<HTMLElement>;
  isSelectMode: boolean;
  isSmallScreen: boolean;
}

const artistIndexSelector = createSelector(
  (state: AppState) => state.artistIndex.bannerOptions,
  (bannerOptions) => {
    return {
      bannerOptions,
    };
  }
);

const Cell: React.FC<GridChildComponentProps<CellItemData>> = ({
  columnIndex,
  rowIndex,
  style,
  data,
}) => {
  const { layout, items, sortKey, isSelectMode } = data;
  const { columnCount, padding, bannerWidth, bannerHeight } = layout;
  const index = rowIndex * columnCount + columnIndex;

  if (index >= items.length) {
    return null;
  }

  const artist = items[index];

  return (
    <div
      style={{
        padding,
        ...style,
      }}
    >
      <ArtistIndexBanner
        artistId={artist.id}
        sortKey={sortKey}
        isSelectMode={isSelectMode}
        bannerWidth={bannerWidth}
        bannerHeight={bannerHeight}
      />
    </div>
  );
};

function getWindowScrollTopPosition() {
  return document.documentElement.scrollTop || document.body.scrollTop || 0;
}

export default function ArtistIndexBanners(props: ArtistIndexBannersProps) {
  const {
    scrollerRef,
    items,
    sortKey,
    jumpToCharacter,
    isSelectMode,
    isSmallScreen,
  } = props;

  const { bannerOptions } = useSelector(artistIndexSelector);
  const ref = useRef<Grid>(null);
  const [measureRef, bounds] = useMeasure();
  const [size, setSize] = useState({ width: 0, height: 0 });

  const columnWidth = useMemo(() => {
    const { width } = size;
    const maximumColumnWidth = isSmallScreen ? 344 : 364;
    const columns = Math.floor(width / maximumColumnWidth);
    const remainder = width % maximumColumnWidth;
    return remainder === 0
      ? maximumColumnWidth
      : Math.floor(
          width / (columns + ADDITIONAL_COLUMN_COUNT[bannerOptions.size])
        );
  }, [isSmallScreen, bannerOptions, size]);

  const columnCount = useMemo(
    () => Math.max(Math.floor(size.width / columnWidth), 1),
    [size, columnWidth]
  );
  const padding = props.isSmallScreen
    ? columnPaddingSmallScreen
    : columnPadding;
  const bannerWidth = columnWidth - padding * 2;
  const bannerHeight = Math.ceil((88 / 476) * bannerWidth);

  const rowHeight = useMemo(() => {
    const {
      detailedProgressBar,
      showTitle,
      showMonitored,
      showQualityProfile,
      showNextAlbum,
    } = bannerOptions;

    const nextAiringHeight = 19;

    const heights = [
      bannerHeight,
      detailedProgressBar ? detailedProgressBarHeight : progressBarHeight,
      nextAiringHeight,
      isSmallScreen ? columnPaddingSmallScreen : columnPadding,
    ];

    if (showTitle) {
      heights.push(19);
    }

    if (showMonitored) {
      heights.push(19);
    }

    if (showQualityProfile) {
      heights.push(19);
    }

    if (showNextAlbum) {
      heights.push(19);
    }

    switch (sortKey) {
      case 'artistType':
      case 'metadataProfileId':
      case 'lastAlbum':
      case 'added':
      case 'albumCount':
      case 'path':
      case 'sizeOnDisk':
      case 'tags':
        heights.push(19);
        break;
      case 'qualityProfileId':
        if (!showQualityProfile) {
          heights.push(19);
        }
        break;
      case 'nextAlbum':
        if (!showNextAlbum) {
          heights.push(19);
        }
        break;
      default:
      // No need to add a height of 0
    }

    return heights.reduce((acc, height) => acc + height, 0);
  }, [isSmallScreen, bannerOptions, sortKey, bannerHeight]);

  useEffect(() => {
    const current = scrollerRef.current;

    if (isSmallScreen) {
      const padding = bodyPaddingSmallScreen - 5;

      setSize({
        width: window.innerWidth - padding * 2,
        height: window.innerHeight,
      });

      return;
    }

    if (current) {
      const width = current.clientWidth;
      const padding = bodyPadding - 5;

      setSize({
        width: width - padding * 2,
        height: window.innerHeight,
      });
    }
  }, [isSmallScreen, scrollerRef, bounds]);

  useEffect(() => {
    const currentScrollerRef = scrollerRef.current as HTMLElement;
    const currentScrollListener = isSmallScreen ? window : currentScrollerRef;

    const handleScroll = throttle(() => {
      const { offsetTop = 0 } = currentScrollerRef;
      const scrollTop =
        (isSmallScreen
          ? getWindowScrollTopPosition()
          : currentScrollerRef.scrollTop) - offsetTop;

      ref.current?.scrollTo({ scrollLeft: 0, scrollTop });
    }, 10);

    currentScrollListener.addEventListener('scroll', handleScroll);

    return () => {
      handleScroll.cancel();

      if (currentScrollListener) {
        currentScrollListener.removeEventListener('scroll', handleScroll);
      }
    };
  }, [isSmallScreen, ref, scrollerRef]);

  useEffect(() => {
    if (jumpToCharacter) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (index != null) {
        const rowIndex = Math.floor(index / columnCount);

        const scrollTop = rowIndex * rowHeight + padding;

        ref.current?.scrollTo({ scrollLeft: 0, scrollTop });
        scrollerRef.current?.scrollTo(0, scrollTop);
      }
    }
  }, [
    jumpToCharacter,
    rowHeight,
    columnCount,
    padding,
    items,
    scrollerRef,
    ref,
  ]);

  return (
    <div ref={measureRef}>
      <Grid<CellItemData>
        ref={ref}
        style={{
          width: '100%',
          height: '100%',
          overflow: 'none',
        }}
        width={size.width}
        height={size.height}
        columnCount={columnCount}
        columnWidth={columnWidth}
        rowCount={Math.ceil(items.length / columnCount)}
        rowHeight={rowHeight}
        itemData={{
          layout: {
            columnCount,
            padding,
            bannerWidth,
            bannerHeight,
          },
          items,
          sortKey,
          isSelectMode,
        }}
      >
        {Cell}
      </Grid>
    </div>
  );
}
