import React from 'react';
import FilterBuilderRowValueProps from 'Components/Filter/Builder/FilterBuilderRowValueProps';
import translate from 'Utilities/String/translate';
import FilterBuilderRowValue from './FilterBuilderRowValue';

const options = [
  {
    id: 'all',
    get name() {
      return translate('AllAlbums');
    },
  },
  {
    id: 'new',
    get name() {
      return translate('New');
    },
  },
  {
    id: 'none',
    get name() {
      return translate('None');
    },
  },
];

function MonitorNewItemsFilterBuilderRowValue(
  props: FilterBuilderRowValueProps
) {
  return <FilterBuilderRowValue tagList={options} {...props} />;
}

export default MonitorNewItemsFilterBuilderRowValue;
