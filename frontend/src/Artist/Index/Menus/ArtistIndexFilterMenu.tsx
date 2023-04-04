import React from 'react';
import { CustomFilter } from 'App/State/AppState';
import ArtistIndexFilterModal from 'Artist/Index/ArtistIndexFilterModal';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';

interface ArtistIndexFilterMenuProps {
  selectedFilterKey: string | number;
  filters: object[];
  customFilters: CustomFilter[];
  isDisabled: boolean;
  onFilterSelect(filterName: string): unknown;
}

function ArtistIndexFilterMenu(props: ArtistIndexFilterMenuProps) {
  const {
    selectedFilterKey,
    filters,
    customFilters,
    isDisabled,
    onFilterSelect,
  } = props;

  return (
    <FilterMenu
      alignMenu={align.RIGHT}
      isDisabled={isDisabled}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={customFilters}
      filterModalConnectorComponent={ArtistIndexFilterModal}
      onFilterSelect={onFilterSelect}
    />
  );
}

ArtistIndexFilterMenu.defaultProps = {
  showCustomFilters: false,
};

export default ArtistIndexFilterMenu;
