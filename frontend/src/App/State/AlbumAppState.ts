import Album from 'Album/Album';
import AppSectionState, {
  AppSectionDeleteState,
} from 'App/State/AppSectionState';

interface AlbumAppState extends AppSectionState<Album>, AppSectionDeleteState {}

export default AlbumAppState;
