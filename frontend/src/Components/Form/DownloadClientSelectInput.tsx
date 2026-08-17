import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { fetchDownloadClients } from 'Store/Actions/settingsActions';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import EnhancedSelectInput from './EnhancedSelectInput';

function createDownloadClientsSelector(includeAny: boolean, protocol: string) {
  return createSelector(
    (state: AppState) => state.settings.downloadClients,
    (downloadClients) => {
      const { isFetching, isPopulated, error, items } = downloadClients;

      const filteredItems = items.filter((item) => item.protocol === protocol);

      const values = filteredItems
        .sort(sortByProp('name'))
        .map((downloadClient) => {
          return {
            key: downloadClient.id,
            value: downloadClient.name,
            hint: `(${downloadClient.id})`,
          };
        });

      if (includeAny) {
        values.unshift({
          key: 0,
          value: `(${translate('Any')})`,
          hint: '',
        });
      }

      return {
        isFetching,
        isPopulated,
        error,
        values,
      };
    }
  );
}

interface DownloadClientSelectInputProps {
  name: string;
  value: number;
  includeAny?: boolean;
  protocol?: string;
  onChange(payload: { name: string; value: number }): void;
}

function DownloadClientSelectInput({
  includeAny = false,
  protocol = 'torrent',
  ...otherProps
}: DownloadClientSelectInputProps) {
  const dispatch = useDispatch();
  const { isFetching, isPopulated, values } = useSelector(
    createDownloadClientsSelector(includeAny, protocol)
  );

  useEffect(() => {
    if (!isPopulated) {
      dispatch(fetchDownloadClients());
    }
  }, [isPopulated, dispatch]);

  return (
    <EnhancedSelectInput
      {...otherProps}
      isFetching={isFetching}
      values={values}
    />
  );
}

export default DownloadClientSelectInput;
