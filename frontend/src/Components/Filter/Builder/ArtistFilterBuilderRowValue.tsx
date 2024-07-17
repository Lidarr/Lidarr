import React from 'react';
import { useSelector } from 'react-redux';
import Artist from 'Artist/Artist';
import createAllArtistSelector from 'Store/Selectors/createAllArtistSelector';
import sortByProp from 'Utilities/Array/sortByProp';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

function ArtistFilterBuilderRowValue(props: FilterBuilderRowValueProps) {
  const allArtists: Artist[] = useSelector(createAllArtistSelector());

  const tagList = allArtists
    .map((artist) => ({ id: artist.id, name: artist.artistName }))
    .sort(sortByProp('name'));

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default ArtistFilterBuilderRowValue;
