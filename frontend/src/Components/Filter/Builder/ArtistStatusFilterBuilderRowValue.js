import React from 'react';
import FilterBuilderRowValue from './FilterBuilderRowValue';

const artistStatusList = [
  { id: 'continuing', name: 'Continuing' },
  { id: 'ended', name: 'Inactive' }
];

function ArtistStatusFilterBuilderRowValue(props) {
  return (
    <FilterBuilderRowValue
      tagList={artistStatusList}
      {...props}
    />
  );
}

export default ArtistStatusFilterBuilderRowValue;
