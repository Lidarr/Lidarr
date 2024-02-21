import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router-dom';
import { createSelector } from 'reselect';
import { fetchTranslations, saveDimensions, setIsSidebarVisible } from 'Store/Actions/appActions';
import { fetchArtist } from 'Store/Actions/artistActions';
import { fetchCustomFilters } from 'Store/Actions/customFilterActions';
import {
  fetchImportLists,
  fetchIndexerFlags,
  fetchLanguages,
  fetchMetadataProfiles,
  fetchQualityProfiles,
  fetchUISettings
} from 'Store/Actions/settingsActions';
import { fetchStatus } from 'Store/Actions/systemActions';
import { fetchTags } from 'Store/Actions/tagActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import ErrorPage from './ErrorPage';
import LoadingPage from './LoadingPage';
import Page from './Page';

function testLocalStorage() {
  const key = 'lidarrTest';

  try {
    localStorage.setItem(key, key);
    localStorage.removeItem(key);

    return true;
  } catch (e) {
    return false;
  }
}

const selectAppProps = createSelector(
  (state) => state.app.isSidebarVisible,
  (state) => state.app.version,
  (state) => state.app.isUpdated,
  (state) => state.app.isDisconnected,
  (isSidebarVisible, version, isUpdated, isDisconnected) => {
    return {
      isSidebarVisible,
      version,
      isUpdated,
      isDisconnected
    };
  }
);

const selectIsPopulated = createSelector(
  (state) => state.customFilters.isPopulated,
  (state) => state.tags.isPopulated,
  (state) => state.settings.ui.isPopulated,
  (state) => state.settings.languages.isPopulated,
  (state) => state.settings.qualityProfiles.isPopulated,
  (state) => state.settings.metadataProfiles.isPopulated,
  (state) => state.settings.importLists.isPopulated,
  (state) => state.settings.indexerFlags.isPopulated,
  (state) => state.system.status.isPopulated,
  (state) => state.app.translations.isPopulated,
  (
    customFiltersIsPopulated,
    tagsIsPopulated,
    uiSettingsIsPopulated,
    languagesIsPopulated,
    qualityProfilesIsPopulated,
    metadataProfilesIsPopulated,
    importListsIsPopulated,
    indexerFlagsIsPopulated,
    systemStatusIsPopulated,
    translationsIsPopulated
  ) => {
    return (
      customFiltersIsPopulated &&
      tagsIsPopulated &&
      uiSettingsIsPopulated &&
      languagesIsPopulated &&
      qualityProfilesIsPopulated &&
      metadataProfilesIsPopulated &&
      importListsIsPopulated &&
      indexerFlagsIsPopulated &&
      systemStatusIsPopulated &&
      translationsIsPopulated
    );
  }
);

const selectErrors = createSelector(
  (state) => state.customFilters.error,
  (state) => state.tags.error,
  (state) => state.settings.ui.error,
  (state) => state.settings.languages.error,
  (state) => state.settings.qualityProfiles.error,
  (state) => state.settings.metadataProfiles.error,
  (state) => state.settings.importLists.error,
  (state) => state.settings.indexerFlags.error,
  (state) => state.system.status.error,
  (state) => state.app.translations.error,
  (
    customFiltersError,
    tagsError,
    uiSettingsError,
    languagesError,
    qualityProfilesError,
    metadataProfilesError,
    importListsError,
    indexerFlagsError,
    systemStatusError,
    translationsError
  ) => {
    const hasError = !!(
      customFiltersError ||
      tagsError ||
      uiSettingsError ||
      languagesError ||
      qualityProfilesError ||
      metadataProfilesError ||
      importListsError ||
      indexerFlagsError ||
      systemStatusError ||
      translationsError
    );

    return {
      hasError,
      customFiltersError,
      tagsError,
      uiSettingsError,
      languagesError,
      qualityProfilesError,
      metadataProfilesError,
      importListsError,
      indexerFlagsError,
      systemStatusError,
      translationsError
    };
  }
);

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.ui.item.enableColorImpairedMode,
    selectIsPopulated,
    selectErrors,
    selectAppProps,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (
      enableColorImpairedMode,
      isPopulated,
      errors,
      app,
      dimensions,
      systemStatus
    ) => {
      return {
        ...app,
        ...errors,
        isPopulated,
        isSmallScreen: dimensions.isSmallScreen,
        authenticationEnabled: systemStatus.authentication !== 'none',
        enableColorImpairedMode
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    dispatchFetchArtist() {
      dispatch(fetchArtist());
    },
    dispatchFetchCustomFilters() {
      dispatch(fetchCustomFilters());
    },
    dispatchFetchTags() {
      dispatch(fetchTags());
    },
    dispatchFetchLanguages() {
      dispatch(fetchLanguages());
    },
    dispatchFetchQualityProfiles() {
      dispatch(fetchQualityProfiles());
    },
    dispatchFetchMetadataProfiles() {
      dispatch(fetchMetadataProfiles());
    },
    dispatchFetchImportLists() {
      dispatch(fetchImportLists());
    },
    dispatchFetchIndexerFlags() {
      dispatch(fetchIndexerFlags());
    },
    dispatchFetchUISettings() {
      dispatch(fetchUISettings());
    },
    dispatchFetchStatus() {
      dispatch(fetchStatus());
    },
    dispatchFetchTranslations() {
      dispatch(fetchTranslations());
    },
    onResize(dimensions) {
      dispatch(saveDimensions(dimensions));
    },
    onSidebarVisibleChange(isSidebarVisible) {
      dispatch(setIsSidebarVisible({ isSidebarVisible }));
    }
  };
}

class PageConnector extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isLocalStorageSupported: testLocalStorage()
    };
  }

  componentDidMount() {
    if (!this.props.isPopulated) {
      this.props.dispatchFetchArtist();
      this.props.dispatchFetchCustomFilters();
      this.props.dispatchFetchTags();
      this.props.dispatchFetchLanguages();
      this.props.dispatchFetchQualityProfiles();
      this.props.dispatchFetchMetadataProfiles();
      this.props.dispatchFetchImportLists();
      this.props.dispatchFetchIndexerFlags();
      this.props.dispatchFetchUISettings();
      this.props.dispatchFetchStatus();
      this.props.dispatchFetchTranslations();
    }
  }

  //
  // Listeners

  onSidebarToggle = () => {
    this.props.onSidebarVisibleChange(!this.props.isSidebarVisible);
  };

  //
  // Render

  render() {
    const {
      isPopulated,
      hasError,
      dispatchFetchArtist,
      dispatchFetchTags,
      dispatchFetchLanguages,
      dispatchFetchQualityProfiles,
      dispatchFetchMetadataProfiles,
      dispatchFetchImportLists,
      dispatchFetchIndexerFlags,
      dispatchFetchUISettings,
      dispatchFetchStatus,
      dispatchFetchTranslations,
      ...otherProps
    } = this.props;

    if (hasError || !this.state.isLocalStorageSupported) {
      return (
        <ErrorPage
          {...this.state}
          {...otherProps}
        />
      );
    }

    if (isPopulated) {
      return (
        <Page
          {...otherProps}
          onSidebarToggle={this.onSidebarToggle}
        />
      );
    }

    return (
      <LoadingPage />
    );
  }
}

PageConnector.propTypes = {
  isPopulated: PropTypes.bool.isRequired,
  hasError: PropTypes.bool.isRequired,
  isSidebarVisible: PropTypes.bool.isRequired,
  dispatchFetchArtist: PropTypes.func.isRequired,
  dispatchFetchCustomFilters: PropTypes.func.isRequired,
  dispatchFetchTags: PropTypes.func.isRequired,
  dispatchFetchLanguages: PropTypes.func.isRequired,
  dispatchFetchQualityProfiles: PropTypes.func.isRequired,
  dispatchFetchMetadataProfiles: PropTypes.func.isRequired,
  dispatchFetchImportLists: PropTypes.func.isRequired,
  dispatchFetchIndexerFlags: PropTypes.func.isRequired,
  dispatchFetchUISettings: PropTypes.func.isRequired,
  dispatchFetchStatus: PropTypes.func.isRequired,
  dispatchFetchTranslations: PropTypes.func.isRequired,
  onSidebarVisibleChange: PropTypes.func.isRequired
};

export default withRouter(
  connect(createMapStateToProps, createMapDispatchToProps)(PageConnector)
);
