import React from 'react';
import Alert from 'Components/Alert';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function ArtistMonitoringOptionsPopoverContent() {
  return (
    <>
      <Alert>
        This is a one time adjustment to set which albums are monitored
      </Alert>
      <DescriptionList>
        <DescriptionListItem
          title={translate('AllAlbums')}
          data={translate('AllAlbumsData')}
        />

        <DescriptionListItem
          title={translate('FutureAlbums')}
          data={translate('FutureAlbumsData')}
        />

        <DescriptionListItem
          title={translate('MissingAlbums')}
          data={translate('MissingAlbumsData')}
        />

        <DescriptionListItem
          title={translate('ExistingAlbums')}
          data={translate('ExistingAlbumsData')}
        />

        <DescriptionListItem
          title={translate('FirstAlbum')}
          data={translate('FirstAlbumData')}
        />

        <DescriptionListItem
          title={translate('LatestAlbum')}
          data={translate('LatestAlbumData')}
        />

        <DescriptionListItem
          title={translate('None')}
          data={translate('NoneData')}
        />
      </DescriptionList>
    </>
  );
}

export default ArtistMonitoringOptionsPopoverContent;
