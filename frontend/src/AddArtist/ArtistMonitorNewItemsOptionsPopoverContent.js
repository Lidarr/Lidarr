import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function ArtistMonitorNewItemsOptionsPopoverContent() {
  return (
    <DescriptionList>
      <DescriptionListItem
        title={translate('AllAlbums')}
        data="Monitor all new albums"
      />

      <DescriptionListItem
        title={translate('NewAlbums')}
        data="Monitor new albums released after the newest existing album"
      />

      <DescriptionListItem
        title={translate('None')}
        data="Don't monitor any new albums"
      />
    </DescriptionList>
  );
}

export default ArtistMonitorNewItemsOptionsPopoverContent;
