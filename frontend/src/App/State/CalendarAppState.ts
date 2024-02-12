import Album from 'Album/Album';
import AppSectionState, {
  AppSectionFilterState,
} from 'App/State/AppSectionState';

interface CalendarAppState
  extends AppSectionState<Album>,
    AppSectionFilterState<Album> {}

export default CalendarAppState;
