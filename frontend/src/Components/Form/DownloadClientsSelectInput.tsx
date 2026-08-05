import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { fetchDownloadClients } from 'Store/Actions/settingsActions';
import sortByProp from 'Utilities/Array/sortByProp';
import EnhancedSelectInput from './EnhancedSelectInput';

const selectDownloadClientValues = createSelector(
  (state: AppState) => state.settings.downloadClients,
  (downloadClients) => {
    const { isPopulated, items } = downloadClients;

    const values = [...items].sort(sortByProp('name')).map((downloadClient) => {
      return {
        key: downloadClient.id,
        value: downloadClient.name,
      };
    });

    return {
      isPopulated,
      values,
    };
  }
);

interface DownloadClientsSelectInputProps {
  name: string;
  value: number[];
  onChange(payload: { name: string; value: number[] }): void;
}

function DownloadClientsSelectInput(props: DownloadClientsSelectInputProps) {
  const dispatch = useDispatch();
  const { isPopulated, values } = useSelector(selectDownloadClientValues);

  useEffect(() => {
    if (!isPopulated) {
      dispatch(fetchDownloadClients());
    }
  }, [isPopulated, dispatch]);

  return <EnhancedSelectInput {...props} values={values} />;
}

export default DownloadClientsSelectInput;
