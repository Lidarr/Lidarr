import { PropertyFilter } from 'App/State/AppState';
import { filterTypes } from 'Helpers/Props';

export interface StatisticsParams {
  rootFolderPaths?: string[];
  rootFolderPathsNot?: boolean;
  tagIds?: number[];
  tagIdsNot?: boolean;
  qualityProfileIds?: number[];
  qualityProfileIdsNot?: boolean;
  metadataProfileIds?: number[];
  metadataProfileIdsNot?: boolean;
  monitored?: boolean;
}

type ListParam =
  | 'rootFolderPaths'
  | 'tagIds'
  | 'qualityProfileIds'
  | 'metadataProfileIds';

// Keys match filterBuilderProps in Store/Actions/statisticsActions
const LIST_PARAMS: Record<string, ListParam> = {
  rootFolderPath: 'rootFolderPaths',
  tags: 'tagIds',
  qualityProfileId: 'qualityProfileIds',
  metadataProfileId: 'metadataProfileIds',
};

// As in Sonarr, the last condition on a field wins; a row with no usable value is skipped
function getStatisticsParams(propertyFilters: PropertyFilter[]) {
  const params: StatisticsParams = {};

  propertyFilters.forEach(({ key, value, type }) => {
    const values = (Array.isArray(value) ? value : [value]).filter(
      (v) => v != null
    );

    if (!values.length) {
      return;
    }

    const isNegated =
      type === filterTypes.NOT_EQUAL || type === filterTypes.NOT_CONTAINS;

    if (key === 'monitored') {
      // Both values: the API has no "any" or "none", so the field is left unfiltered
      if (new Set(values.map(Boolean)).size > 1) {
        delete params.monitored;
      } else {
        params.monitored = isNegated ? !values[0] : Boolean(values[0]);
      }

      return;
    }

    const listParam = LIST_PARAMS[key];

    if (!listParam) {
      return;
    }

    if (listParam === 'rootFolderPaths') {
      params.rootFolderPaths = values.map(String);
    } else {
      // With no tags yet the builder takes free text, which is no id: the API rejects NaN
      const ids = values.map(Number).filter(Number.isFinite);

      if (!ids.length) {
        return;
      }

      params[listParam] = ids;
    }

    // A missing flag must stay absent: jQuery sends undefined as an empty value, which a bool won't bind
    if (isNegated) {
      params[`${listParam}Not`] = true;
    } else {
      delete params[`${listParam}Not`];
    }
  });

  return params;
}

export default getStatisticsParams;
