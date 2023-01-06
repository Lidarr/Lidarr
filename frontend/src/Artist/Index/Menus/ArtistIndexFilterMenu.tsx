import PropTypes from 'prop-types';
import React from 'react';
import ArtistIndexFilterModal from 'Artist/Index/ArtistIndexFilterModal';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';

function ArtistIndexFilterMenu(props) {
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

ArtistIndexFilterMenu.propTypes = {
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number])
    .isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  isDisabled: PropTypes.bool.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
};

ArtistIndexFilterMenu.defaultProps = {
  showCustomFilters: false,
};

export default ArtistIndexFilterMenu;
