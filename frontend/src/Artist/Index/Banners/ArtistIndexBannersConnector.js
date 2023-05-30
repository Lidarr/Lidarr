import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import ArtistIndexBanners from './ArtistIndexBanners';

function createMapStateToProps() {
  return createSelector(
    (state) => state.artistIndex.bannerOptions,
    createUISettingsSelector(),
    createDimensionsSelector(),
    (bannerOptions, uiSettings, dimensions) => {
      return {
        bannerOptions,
        showRelativeDates: uiSettings.showRelativeDates,
        shortDateFormat: uiSettings.shortDateFormat,
        longDateFormat: uiSettings.longDateFormat,
        timeFormat: uiSettings.timeFormat,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

export default connect(createMapStateToProps)(ArtistIndexBanners);
