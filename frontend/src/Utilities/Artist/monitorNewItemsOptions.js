import translate from 'Utilities/String/translate';

const monitorNewItemsOptions = [
  {
    key: 'all',
    get value() {
      return translate('AllAlbums');
    }
  },
  {
    key: 'none',
    get value() {
      return translate('None');
    }
  },
  {
    key: 'new',
    get value() {
      return translate('New');
    }
  }
];

export default monitorNewItemsOptions;
