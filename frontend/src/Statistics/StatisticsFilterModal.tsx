import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setStatisticsFilter } from 'Store/Actions/statisticsActions';

interface StatisticsFilterModalProps {
  isOpen: boolean;
}

export default function StatisticsFilterModal(
  props: StatisticsFilterModalProps
) {
  const rootFolders = useSelector(
    (state: AppState) => state.settings.rootFolders.items
  );
  const filterBuilderProps = useSelector(
    (state: AppState) => state.statistics.filterBuilderProps
  );

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload: unknown) => {
      dispatch(setStatisticsFilter(payload));
    },
    [dispatch]
  );

  return (
    <FilterModal
      {...props}
      sectionItems={rootFolders}
      filterBuilderProps={filterBuilderProps}
      customFilterType="statistics"
      dispatchSetFilter={dispatchSetFilter}
    />
  );
}
