import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import Plugins from './Plugins';

function createMapStateToProps() {
  return createSelector(
    createCommandExecutingSelector(commandNames.INSTALL_PLUGIN),
    (
      isInstallingPlugin
    ) => {

      return {
        isInstallingPlugin
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchExecuteCommand: executeCommand
};

class PluginsConnector extends Component {
  //
  // Listeners

  onInstallPluginPress = (url) => {
    this.props.dispatchExecuteCommand({
      name: commandNames.INSTALL_PLUGIN,
      githubUrl: url
    });
  }

  //
  // Render

  render() {
    return (
      <Plugins
        onInstallPluginPress={this.onInstallPluginPress}
        {...this.props}
      />
    );
  }

}

PluginsConnector.propTypes = {
  dispatchExecuteCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(PluginsConnector);
