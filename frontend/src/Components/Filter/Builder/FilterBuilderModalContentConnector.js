import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteCustomFilter, saveCustomFilter } from 'Store/Actions/customFilterActions';
import { fetchDownloadClients, fetchIndexers } from 'Store/Actions/settingsActions';
import FilterBuilderModalContent from './FilterBuilderModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { customFilters }) => customFilters,
    (state, { id }) => id,
    (state) => state.customFilters.isSaving,
    (state) => state.customFilters.saveError,
    (state) => state.settings.downloadClients.isPopulated,
    (state) => state.settings.indexers.isPopulated,
    (customFilters, id, isSaving, saveError, downloadClientsPopulated, indexersPopulated) => {
      const isPopulated = downloadClientsPopulated && indexersPopulated;

      if (id) {
        const customFilter = customFilters.find((c) => c.id === id);

        return {
          id: customFilter.id,
          label: customFilter.label,
          filters: customFilter.filters,
          customFilters,
          isSaving,
          saveError,
          isPopulated
        };
      }

      return {
        label: '',
        filters: [],
        customFilters,
        isSaving,
        saveError,
        isPopulated
      };
    }
  );
}

const mapDispatchToProps = {
  onSaveCustomFilterPress: saveCustomFilter,
  dispatchDeleteCustomFilter: deleteCustomFilter,
  dispatchFetchDownloadClients: fetchDownloadClients,
  dispatchFetchIndexers: fetchIndexers
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterBuilderModalContent);
