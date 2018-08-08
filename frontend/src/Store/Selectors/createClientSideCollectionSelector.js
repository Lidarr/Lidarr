import _ from 'lodash';
import { createSelector } from 'reselect';
import findSelectedFilters from 'Utilities/Filter/findSelectedFilters';
import { filterTypePredicates, filterTypes, sortDirections } from 'Helpers/Props';

function getSortClause(sortKey, sortDirection, sortPredicates) {
  if (sortPredicates && sortPredicates.hasOwnProperty(sortKey)) {
    return function(item) {
      return sortPredicates[sortKey](item, sortDirection);
    };
  }

  return function(item) {
    return item[sortKey];
  };
}

function filter(items, state) {
  const {
    selectedFilterKey,
    filters,
    customFilters,
    filterPredicates
  } = state;

  if (!selectedFilterKey) {
    return items;
  }

  const selectedFilters = findSelectedFilters(selectedFilterKey, filters, customFilters);

  return _.filter(items, (item) => {
    let i = 0;
    let accepted = true;

    while (accepted && i < selectedFilters.length) {
      const {
        key,
        value,
        type = filterTypes.EQUAL
      } = selectedFilters[i];

      if (filterPredicates && filterPredicates.hasOwnProperty(key)) {
        const predicate = filterPredicates[key];

        if (Array.isArray(value)) {
          accepted = value.some((v) => predicate(item, v, type));
        } else {
          accepted = predicate(item, value, type);
        }
      } else if (item.hasOwnProperty(key)) {
        const predicate = filterTypePredicates[type];

        if (Array.isArray(value)) {
          accepted = value.some((v) => predicate(item[key], v));
        } else {
          accepted = predicate(item[key], value);
        }
      } else {
        // Default to false if the filter can't be tested
        accepted = false;
      }

      i++;
    }

    return accepted;
  });
}

function sort(items, state) {
  const {
    sortKey,
    sortDirection,
    sortPredicates,
    secondarySortKey,
    secondarySortDirection
  } = state;

  const clauses = [];
  const orders = [];

  clauses.push(getSortClause(sortKey, sortDirection, sortPredicates));
  orders.push(sortDirection === sortDirections.ASCENDING ? 'asc' : 'desc');

  if (secondarySortKey &&
      secondarySortDirection &&
      (sortKey !== secondarySortKey ||
       sortDirection !== secondarySortDirection)) {
    clauses.push(getSortClause(secondarySortKey, secondarySortDirection, sortPredicates));
    orders.push(secondarySortDirection === sortDirections.ASCENDING ? 'asc' : 'desc');
  }

  return _.orderBy(items, clauses, orders);
}

function createClientSideCollectionSelector(section, uiSection) {
  return createSelector(
    (state) => _.get(state, section),
    (state) => _.get(state, uiSection),
    (sectionState, uiSectionState = {}) => {
      const state = Object.assign({}, sectionState, uiSectionState);

      const filtered = filter(state.items, state);
      const sorted = sort(filtered, state);

      return {
        ...sectionState,
        ...uiSectionState,
        items: sorted,
        totalItems: state.items.length
      };
    }
  );
}

export default createClientSideCollectionSelector;
