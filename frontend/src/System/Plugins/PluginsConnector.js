import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchInstalledPlugins } from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import Plugins from './Plugins';

function createMapStateToProps() {
  return createSelector(
    (state) => state.system.plugins,
    (state) => state.app.isReconnecting,
    createCommandExecutingSelector(commandNames.INSTALL_PLUGIN),
    createCommandExecutingSelector(commandNames.UNINSTALL_PLUGIN),
    (
      plugins,
      isReconnecting,
      isInstallingPlugin,
      isUninstallingPlugin
    ) => {
      return {
        ...plugins,
        isReconnecting,
        isInstallingPlugin,
        isUninstallingPlugin
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchFetchInstalledPlugins: fetchInstalledPlugins,
  dispatchExecuteCommand: executeCommand
};

class PluginsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchInstalledPlugins();
  }

  componentDidUpdate(prevProps) {
    console.log('Not reconnected');
    if (prevProps.isReconnecting && !this.props.isReconnecting) {
      console.log('reconnected');
      this.props.dispatchFetchInstalledPlugins();
    }
  }

  //
  // Listeners

  onInstallPluginPress = (url) => {
    this.props.dispatchExecuteCommand({
      name: commandNames.INSTALL_PLUGIN,
      githubUrl: url
    });
  }

  onUninstallPluginPress = (url) => {
    this.props.dispatchExecuteCommand({
      name: commandNames.UNINSTALL_PLUGIN,
      githubUrl: url
    });
  }

  //
  // Render

  render() {
    return (
      <Plugins
        onInstallPluginPress={this.onInstallPluginPress}
        onUninstallPluginPress={this.onUninstallPluginPress}
        {...this.props}
      />
    );
  }

}

PluginsConnector.propTypes = {
  dispatchFetchInstalledPlugins: PropTypes.func.isRequired,
  dispatchExecuteCommand: PropTypes.func.isRequired,
  isReconnecting: PropTypes.bool.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(PluginsConnector);
