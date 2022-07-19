import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchInstalledPlugins } from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import Plugins from './Plugins';

function createMapStateToProps() {
  return createSelector(
    (state) => state.system.plugins,
    createCommandExecutingSelector(commandNames.INSTALL_PLUGIN),
    createCommandExecutingSelector(commandNames.UNINSTALL_PLUGIN),
    (
      plugins,
      isInstallingPlugin,
      isUninstallingPlugin
    ) => {
      return {
        ...plugins,
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
    registerPagePopulator(this.repopulate);

    this.repopulate();
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.repopulate);
  }

  //
  // Control

  repopulate = () => {
    this.props.dispatchFetchInstalledPlugins();
  };

  //
  // Listeners

  onInstallPluginPress = (url) => {
    this.props.dispatchExecuteCommand({
      name: commandNames.INSTALL_PLUGIN,
      githubUrl: url
    });
  };

  onUninstallPluginPress = (url) => {
    this.props.dispatchExecuteCommand({
      name: commandNames.UNINSTALL_PLUGIN,
      githubUrl: url
    });
  };

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
  dispatchExecuteCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(PluginsConnector);
