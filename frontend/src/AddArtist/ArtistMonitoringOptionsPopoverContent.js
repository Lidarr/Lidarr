import React from 'react';
import Alert from 'Components/Alert';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function ArtistMonitoringOptionsPopoverContent() {
  return (
    <>
      <Alert>
        {translate('MonitorOneTimeAdjustmentAlert')}
      </Alert>
      <DescriptionList>
        <DescriptionListItem
          title={translate('AllAlbums')}
          data={translate('MonitorAllAlbumsData')}
        />

        <DescriptionListItem
          title={translate('FutureAlbums')}
          data={translate('MonitorFutureAlbumsData')}
        />

        <DescriptionListItem
          title={translate('MissingAlbums')}
          data={translate('MonitorMissingAlbumsData')}
        />

        <DescriptionListItem
          title={translate('ExistingAlbums')}
          data={translate('MonitorExistingAlbumsData')}
        />

        <DescriptionListItem
          title={translate('FirstAlbum')}
          data={translate('MonitorFirstAlbumData')}
        />

        <DescriptionListItem
          title={translate('LatestAlbum')}
          data={translate('MonitorLatestAlbumData')}
        />

        <DescriptionListItem
          title={translate('None')}
          data={translate('MonitorNoneData')}
        />
      </DescriptionList>
    </>
  );
}

export default ArtistMonitoringOptionsPopoverContent;
