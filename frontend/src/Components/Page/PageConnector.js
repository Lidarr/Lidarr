/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router-dom';
import { createSelector } from 'reselect';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import { saveDimensions, setIsSidebarVisible } from 'Store/Actions/appActions';
import { fetchArtist } from 'Store/Actions/artistActions';
import { fetchTags } from 'Store/Actions/tagActions';
import { fetchQualityProfiles, fetchLanguageProfiles, fetchMetadataProfiles, fetchUISettings } from 'Store/Actions/settingsActions';
import { fetchStatus } from 'Store/Actions/systemActions';
import ErrorPage from './ErrorPage';
import LoadingPage from './LoadingPage';
import Page from './Page';

function testLocalStorage() {
  const key = 'sonarrTest';

  try {
    localStorage.setItem(key, key);
    localStorage.removeItem(key);

    return true;
  } catch (e) {
    return false;
  }
}

function createMapStateToProps() {
  return createSelector(
    (state) => state.artist,
    (state) => state.tags,
    (state) => state.settings,
    (state) => state.app,
    createDimensionsSelector(),
    (artist, tags, settings, app, dimensions) => {
      const isPopulated = artist.isPopulated &&
        tags.isPopulated &&
        settings.qualityProfiles.isPopulated &&
        settings.ui.isPopulated;

      const hasError = !!artist.error ||
        !!tags.error ||
        !!settings.qualityProfiles.error ||
        !!settings.ui.error;

      return {
        isPopulated,
        hasError,
        artistError: artist.error,
        tagsError: tags.error,
        qualityProfilesError: settings.qualityProfiles.error,
        uiSettingsError: settings.ui.error,
        isSmallScreen: dimensions.isSmallScreen,
        isSidebarVisible: app.isSidebarVisible,
        version: app.version,
        isUpdated: app.isUpdated,
        isDisconnected: app.isDisconnected
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    dispatchFetchSeries() {
      dispatch(fetchArtist());
    },
    dispatchFetchTags() {
      dispatch(fetchTags());
    },
    dispatchFetchQualityProfiles() {
      dispatch(fetchQualityProfiles());
    },
    dispatchFetchLanguageProfiles() {
      dispatch(fetchLanguageProfiles());
    },
    dispatchFetchMetadataProfiles() {
      dispatch(fetchMetadataProfiles());
    },
    dispatchFetchUISettings() {
      dispatch(fetchUISettings());
    },
    dispatchFetchStatus() {
      dispatch(fetchStatus());
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
      this.props.dispatchFetchSeries();
      this.props.dispatchFetchTags();
      this.props.dispatchFetchQualityProfiles();
      this.props.dispatchFetchLanguageProfiles();
      this.props.dispatchFetchMetadataProfiles();
      this.props.dispatchFetchUISettings();
      this.props.dispatchFetchStatus();
    }
  }

  //
  // Listeners

  onSidebarToggle = () => {
    this.props.onSidebarVisibleChange(!this.props.isSidebarVisible);
  }

  //
  // Render

  render() {
    const {
      isPopulated,
      hasError,
      dispatchFetchSeries,
      dispatchFetchTags,
      dispatchFetchQualityProfiles,
      dispatchFetchLanguageProfiles,
      dispatchFetchMetadataProfiles,
      dispatchFetchUISettings,
      dispatchFetchStatus,
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
  dispatchFetchSeries: PropTypes.func.isRequired,
  dispatchFetchTags: PropTypes.func.isRequired,
  dispatchFetchQualityProfiles: PropTypes.func.isRequired,
  dispatchFetchLanguageProfiles: PropTypes.func.isRequired,
  dispatchFetchMetadataProfiles: PropTypes.func.isRequired,
  dispatchFetchUISettings: PropTypes.func.isRequired,
  dispatchFetchStatus: PropTypes.func.isRequired,
  onSidebarVisibleChange: PropTypes.func.isRequired
};

export default withRouter(
  connect(createMapStateToProps, createMapDispatchToProps)(PageConnector)
);
