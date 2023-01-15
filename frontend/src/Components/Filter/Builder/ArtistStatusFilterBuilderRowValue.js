import React from 'react';
import FilterBuilderRowValue from './FilterBuilderRowValue';

const protocols = [
  { id: 'continuing', name: 'Continuing' },
  { id: 'ended', name: 'Inactive' }
];

function ArtistStatusFilterBuilderRowValue(props) {
  return (
    <FilterBuilderRowValue
      tagList={protocols}
      {...props}
    />
  );
}

export default ArtistStatusFilterBuilderRowValue;
