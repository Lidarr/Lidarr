import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

const selectBannerOptions = createSelector(
  (state: AppState) => state.artistIndex.bannerOptions,
  (bannerOptions) => bannerOptions
);

export default selectBannerOptions;
