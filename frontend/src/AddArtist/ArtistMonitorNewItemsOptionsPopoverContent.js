import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function ArtistMonitorNewItemsOptionsPopoverContent() {
  return (
    <DescriptionList>
      <DescriptionListItem
        title={translate('AllAlbums')}
        data={translate('MonitorAllAlbums')}
      />

      <DescriptionListItem
        title={translate('NewAlbums')}
        data={translate('MonitorNewAlbumsData')}
      />

      <DescriptionListItem
        title={translate('None')}
        data={translate('MonitorNoAlbumsData')}
      />
    </DescriptionList>
  );
}

export default ArtistMonitorNewItemsOptionsPopoverContent;
