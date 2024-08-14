import React from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import FilterBuilderRowValueProps from 'Components/Filter/Builder/FilterBuilderRowValueProps';
import sortByProp from 'Utilities/Array/sortByProp';
import FilterBuilderRowValue from './FilterBuilderRowValue';

function createMetadataProfilesSelector() {
  return createSelector(
    (state: AppState) => state.settings.metadataProfiles.items,
    (metadataProfiles) => {
      return metadataProfiles;
    }
  );
}

function MetadataProfileFilterBuilderRowValue(
  props: FilterBuilderRowValueProps
) {
  const metadataProfiles = useSelector(createMetadataProfilesSelector());

  const tagList = metadataProfiles
    .map(({ id, name }) => ({ id, name }))
    .sort(sortByProp('name'));

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default MetadataProfileFilterBuilderRowValue;
