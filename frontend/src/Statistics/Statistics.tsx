import React, { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import FilterMenu from 'Components/Menu/FilterMenu';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import { align, kinds } from 'Helpers/Props';
import { fetchRootFolders } from 'Store/Actions/settingsActions';
import {
  fetchStatistics,
  setStatisticsFilter,
} from 'Store/Actions/statisticsActions';
import { createCustomFiltersSelector } from 'Store/Selectors/createClientSideCollectionSelector';
import findSelectedFilters from 'Utilities/Filter/findSelectedFilters';
import translate from 'Utilities/String/translate';
import getStatisticsParams from './getStatisticsParams';
import StatisticsContent from './StatisticsContent';
import StatisticsFilterModal from './StatisticsFilterModal';
import styles from './Statistics.css';

const customFiltersSelector = createCustomFiltersSelector('statistics');

function Statistics() {
  const dispatch = useDispatch();

  const {
    isFetching,
    isPopulated,
    error,
    item: data,
    selectedFilterKey,
    filters,
  } = useSelector((state: AppState) => state.statistics);

  const customFilters = useSelector(customFiltersSelector);

  const params = useMemo(() => {
    return getStatisticsParams(
      findSelectedFilters(selectedFilterKey, filters, customFilters)
    );
  }, [selectedFilterKey, filters, customFilters]);

  useEffect(() => {
    dispatch(fetchRootFolders());
  }, [dispatch]);

  useEffect(() => {
    dispatch(fetchStatistics(params));
  }, [dispatch, params]);

  const handleFilterSelect = useCallback(
    (key: string | number) => {
      dispatch(setStatisticsFilter({ selectedFilterKey: key }));
    },
    [dispatch]
  );

  // As in Sonarr, a filter change keeps the old numbers on screen until the new ones arrive
  const isLoading = !isPopulated && (isFetching || !error);

  return (
    <PageContent title={translate('Statistics')}>
      <PageToolbar>
        <PageToolbarSection alignContent={align.RIGHT}>
          <FilterMenu
            alignMenu={align.RIGHT}
            selectedFilterKey={selectedFilterKey}
            filters={filters}
            customFilters={customFilters}
            filterModalConnectorComponent={StatisticsFilterModal}
            onFilterSelect={handleFilterSelect}
          />
        </PageToolbarSection>
      </PageToolbar>

      <PageContentBody>
        {isLoading ? <LoadingIndicator /> : null}

        {!isLoading && error ? (
          <Alert kind={kinds.DANGER}>{translate('StatisticsLoadError')}</Alert>
        ) : null}

        {isPopulated ? (
          <div className={styles.chartsContainer}>
            <StatisticsContent data={data} />
          </div>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default Statistics;
