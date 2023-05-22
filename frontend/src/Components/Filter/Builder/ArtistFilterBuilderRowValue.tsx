import React from 'react';
import { useSelector } from 'react-redux';
import Artist from 'Artist/Artist';
import createAllArtistSelector from 'Store/Selectors/createAllArtistSelector';
import sortByName from 'Utilities/Array/sortByName';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

function ArtistFilterBuilderRowValue(props: FilterBuilderRowValueProps) {
  const allArtists: Artist[] = useSelector(createAllArtistSelector());

  const tagList = allArtists
    .map((artist) => ({ id: artist.id, name: artist.artistName }))
    .sort(sortByName);

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default ArtistFilterBuilderRowValue;
