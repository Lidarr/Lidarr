import React from 'react';
import translate from 'Utilities/String/translate';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

const EVENT_TYPE_OPTIONS = [
  {
    id: 1,
    get name() {
      return translate('Grabbed');
    },
  },
  {
    id: 3,
    get name() {
      return translate('TrackImported');
    },
  },
  {
    id: 4,
    get name() {
      return translate('DownloadFailed');
    },
  },
  {
    id: 7,
    get name() {
      return translate('ImportCompleteFailed');
    },
  },
  {
    id: 8,
    get name() {
      return translate('DownloadImported');
    },
  },
  {
    id: 5,
    get name() {
      return translate('Deleted');
    },
  },
  {
    id: 6,
    get name() {
      return translate('Renamed');
    },
  },
  {
    id: 9,
    get name() {
      return translate('Retagged');
    },
  },
  {
    id: 7,
    get name() {
      return translate('Ignored');
    },
  },
];

function HistoryEventTypeFilterBuilderRowValue(
  props: FilterBuilderRowValueProps
) {
  return <FilterBuilderRowValue {...props} tagList={EVENT_TYPE_OPTIONS} />;
}

export default HistoryEventTypeFilterBuilderRowValue;
