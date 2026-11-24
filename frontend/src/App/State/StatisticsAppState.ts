import RootFolder from 'typings/RootFolder';
import Statistics from 'typings/Statistics';
import { AppSectionFilterState, AppSectionItemState } from './AppSectionState';
import { Filter } from './AppState';

interface StatisticsAppState
  extends AppSectionItemState<Statistics>,
    AppSectionFilterState<RootFolder> {
  selectedFilterKey: string | number;
  filters: Filter[];
}

export default StatisticsAppState;
