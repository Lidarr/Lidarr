import AppSectionState, {
  AppSectionDeleteState,
} from 'App/State/AppSectionState';
import { TrackFile } from 'TrackFile/TrackFile';

interface TrackFilesAppState
  extends AppSectionState<TrackFile>,
    AppSectionDeleteState {}

export default TrackFilesAppState;
