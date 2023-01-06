import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setArtistFilter } from 'Store/Actions/artistIndexActions';

function createArtistSelector() {
  return createSelector(
    (state: AppState) => state.artist.items,
    (artist) => {
      return artist;
    }
  );
}

function createFilterBuilderPropsSelector() {
  return createSelector(
    (state: AppState) => state.artistIndex.filterBuilderProps,
    (filterBuilderProps) => {
      return filterBuilderProps;
    }
  );
}

export default function ArtistIndexFilterModal(props) {
  const sectionItems = useSelector(createArtistSelector());
  const filterBuilderProps = useSelector(createFilterBuilderPropsSelector());
  const customFilterType = 'artist';

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload) => {
      dispatch(setArtistFilter(payload));
    },
    [dispatch]
  );

  return (
    <FilterModal
      {...props}
      sectionItems={sectionItems}
      filterBuilderProps={filterBuilderProps}
      customFilterType={customFilterType}
      dispatchSetFilter={dispatchSetFilter}
    />
  );
}
