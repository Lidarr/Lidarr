import translate from 'Utilities/String/translate';

const monitorNewItemsOptions = [
  {
    key: 'all',
    get value() {
      return translate('MonitorAllAlbums');
    }
  },
  {
    key: 'none',
    get value() {
      return translate('MonitorNoNewAlbums');
    }
  },
  {
    key: 'new',
    get value() {
      return translate('MonitorNewAlbums');
    }
  }
];

export default monitorNewItemsOptions;
