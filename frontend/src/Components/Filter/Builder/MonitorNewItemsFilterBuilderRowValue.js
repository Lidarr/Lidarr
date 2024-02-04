import React from 'react';
import FilterBuilderRowValue from './FilterBuilderRowValue';

const options = [
  { id: 'all', name: 'All Albums' },
  { id: 'new', name: 'New' },
  { id: 'none', name: 'None' }
];

function MonitorNewItemsFilterBuilderRowValue(props) {
  return (
    <FilterBuilderRowValue
      tagList={options}
      {...props}
    />
  );
}

export default MonitorNewItemsFilterBuilderRowValue;
