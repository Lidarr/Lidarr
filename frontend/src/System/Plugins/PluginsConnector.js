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

  constructor(props, context) {
    super(props, context);

    this.state = {
      isRestartRequiredModalOpen: false,
      pluginOwner: '',
      pluginName: '',
      pluginVersion: '',
      pluginAction: '',
      pluginDetailsUrl: '',
      pluginBranch: ''
    };
  }

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
    this.currentPluginOperation = { action: 'install', url };
    this.props.dispatchExecuteCommand({
      name: commandNames.INSTALL_PLUGIN,
      githubUrl: url,
      commandFinished: this.onPluginCommandFinished
    });
  };

  onUninstallPluginPress = (url) => {
    this.currentPluginOperation = { action: 'uninstall', url };
    this.props.dispatchExecuteCommand({
      name: commandNames.UNINSTALL_PLUGIN,
      githubUrl: url,
      commandFinished: this.onPluginCommandFinished
    });
  };

  onPluginCommandFinished = (command) => {
    let pluginOwner = '';
    let pluginName = '';
    let pluginVersion = '';
    let pluginAction = '';
    let pluginDetailsUrl = '';
    let pluginBranch = '';

    if (this.currentPluginOperation && command) {
      const url = this.currentPluginOperation.url;

      const match = url.match(/github\.com\/([^/]+)\/([^/]+)/);
      if (match) {
        [, pluginOwner, pluginName] = match;
        pluginAction = this.currentPluginOperation.action;

        // Extract branch from GitHub URL
        if (url.includes('/tree/')) {
          const branchMatch = url.match(/\/tree\/([^/]+)/);
          if (branchMatch) {
            pluginBranch = branchMatch[1];
          }
        }

        if (this.currentPluginOperation.action === 'install') {
          if (command && command.message) {
            const pluginMatch = command.message.match(/Plugin \[([^/]+)\/([^\]]+)\] v([0-9.]+) installed/);
            if (pluginMatch) {
              pluginVersion = pluginMatch[3];
            }
            console.log('Plugin match result:', pluginMatch);
          }
          pluginDetailsUrl = url;
        } else {
          if (command && command.message) {
            const pluginMatch = command.message.match(/Plugin \[([^/]+)\/([^\]]+)\] v([0-9.]+) uninstalled/);
            if (pluginMatch) {
              pluginVersion = pluginMatch[3];
            }
          }
        }
      }
    }

    this.setState({
      isRestartRequiredModalOpen: true,
      pluginOwner,
      pluginName,
      pluginVersion,
      pluginAction,
      pluginDetailsUrl,
      pluginBranch
    });
    this.repopulate();
  };

  onCloseRestartRequiredModal = () => {
    this.setState({
      isRestartRequiredModalOpen: false,
      pluginOwner: '',
      pluginName: '',
      pluginVersion: '',
      pluginAction: '',
      pluginDetailsUrl: '',
      pluginBranch: ''
    });
    this.currentPluginOperation = null;
  };

  //
  // Render

  render() {
    return (
      <Plugins
        isRestartRequiredModalOpen={this.state.isRestartRequiredModalOpen}
        pluginOwner={this.state.pluginOwner}
        pluginName={this.state.pluginName}
        pluginVersion={this.state.pluginVersion}
        pluginAction={this.state.pluginAction}
        pluginDetailsUrl={this.state.pluginDetailsUrl}
        pluginBranch={this.state.pluginBranch}
        onInstallPluginPress={this.onInstallPluginPress}
        onUninstallPluginPress={this.onUninstallPluginPress}
        onCloseRestartRequiredModal={this.onCloseRestartRequiredModal}
        {...this.props}
      />
    );
  }

}

PluginsConnector.propTypes = {
  dispatchFetchInstalledPlugins: PropTypes.func.isRequired,
  dispatchExecuteCommand: PropTypes.func.isRequired,
  items: PropTypes.array
};

export default connect(createMapStateToProps, mapDispatchToProps)(PluginsConnector);
