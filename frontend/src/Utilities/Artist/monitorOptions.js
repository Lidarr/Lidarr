import translate from 'Utilities/String/translate';

const monitorOptions = [
  {
    key: 'all',
    get value() {
      return translate('MonitorAllAlbums');
    }
  },
  {
    key: 'future',
    get value() {
      return translate('MonitorFutureAlbums');
    }
  },
  {
    key: 'missing',
    get value() {
      return translate('MonitorMissingAlbums');
    }
  },
  {
    key: 'existing',
    get value() {
      return translate('MonitorExistingAlbums');
    }
  },
  {
    key: 'first',
    get value() {
      return translate('MonitorFirstAlbum');
    }
  },
  {
    key: 'latest',
    get value() {
      return translate('MonitorLastestAlbum');
    }
  },
  {
    key: 'none',
    get value() {
      return translate('MonitorNoAlbums');
    }
  }
];

export default monitorOptions;
