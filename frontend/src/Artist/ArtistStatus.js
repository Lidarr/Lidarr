import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

export function getArtistStatusDetails(status, artistType) {

  let statusDetails = {
    icon: icons.ARTIST_CONTINUING,
    title: translate('Continuing'),
    message: translate('ContinuingMoreAlbumsAreExpected')
  };

  if (status === 'deleted') {
    statusDetails = {
      icon: icons.ARTIST_DELETED,
      title: translate('Deleted'),
      message: translate('ArtistWasDeletedFromMusicBrainz')
    };
  } else if (status === 'ended') {
    statusDetails = {
      icon: icons.ARTIST_ENDED,
      title: artistType === 'Person' ? translate('Deceased') : translate('Inactive'),
      message: translate('ContinuingNoAdditionalAlbumsAreExpected')
    };
  }

  return statusDetails;
}
