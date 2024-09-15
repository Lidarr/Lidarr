import React from 'react';
import MenuContent from 'Components/Menu/MenuContent';
import SortMenu from 'Components/Menu/SortMenu';
import SortMenuItem from 'Components/Menu/SortMenuItem';
import { align } from 'Helpers/Props';
import SortDirection from 'Helpers/Props/SortDirection';
import translate from 'Utilities/String/translate';

interface SeriesIndexSortMenuProps {
  sortKey?: string;
  sortDirection?: SortDirection;
  isDisabled: boolean;
  onSortSelect(sortKey: string): unknown;
}

function ArtistIndexSortMenu(props: SeriesIndexSortMenuProps) {
  const { sortKey, sortDirection, isDisabled, onSortSelect } = props;

  return (
    <SortMenu isDisabled={isDisabled} alignMenu={align.RIGHT}>
      <MenuContent>
        <SortMenuItem
          name="status"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('MonitoredStatus')}
        </SortMenuItem>

        <SortMenuItem
          name="sortName"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Name')}
        </SortMenuItem>

        <SortMenuItem
          name="artistType"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Type')}
        </SortMenuItem>

        <SortMenuItem
          name="qualityProfileId"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('QualityProfile')}
        </SortMenuItem>

        <SortMenuItem
          name="metadataProfileId"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('MetadataProfile')}
        </SortMenuItem>

        <SortMenuItem
          name="nextAlbum"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('NextAlbum')}
        </SortMenuItem>

        <SortMenuItem
          name="lastAlbum"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Last Album')}
        </SortMenuItem>

        <SortMenuItem
          name="added"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Added')}
        </SortMenuItem>

        <SortMenuItem
          name="albumCount"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Albums')}
        </SortMenuItem>

        <SortMenuItem
          name="trackProgress"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Tracks')}
        </SortMenuItem>

        <SortMenuItem
          name="trackCount"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('TrackCount')}
        </SortMenuItem>

        <SortMenuItem
          name="path"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Path')}
        </SortMenuItem>

        <SortMenuItem
          name="sizeOnDisk"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('SizeOnDisk')}
        </SortMenuItem>

        <SortMenuItem
          name="tags"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Tags')}
        </SortMenuItem>

        <SortMenuItem
          name="ratings"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Rating')}
        </SortMenuItem>
      </MenuContent>
    </SortMenu>
  );
}

export default ArtistIndexSortMenu;
