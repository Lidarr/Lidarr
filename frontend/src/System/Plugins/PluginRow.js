import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons, kinds } from 'Helpers/Props';
import styles from './PluginRow.css';

class PluginRow extends Component {

  //
  // Listeners

  onInstallPluginPress = () => {
    this.props.onInstallPluginPress(this.props.githubUrl);
  };

  onUninstallPluginPress = () => {
    this.props.onUninstallPluginPress(this.props.githubUrl);
  };

  //
  // Render

  render() {
    const {
      name,
      owner,
      installedVersion,
      availableVersion,
      updateAvailable,
      isInstallingPlugin,
      isUninstallingPlugin
    } = this.props;

    return (
      <TableRow>
        <TableRowCell>{name}</TableRowCell>
        <TableRowCell>{owner}</TableRowCell>
        <TableRowCell className={styles.version}>{installedVersion}</TableRowCell>
        <TableRowCell className={styles.version}>{availableVersion}</TableRowCell>
        <TableRowCell
          className={styles.actions}
        >
          {
            updateAvailable &&
              <SpinnerIconButton
                name={icons.UPDATE}
                kind={kinds.DEFAULT}
                isSpinning={isInstallingPlugin}
                onPress={this.onInstallPluginPress}
              />
          }
          <SpinnerIconButton
            name={icons.DELETE}
            kind={kinds.DEFAULT}
            isSpinning={isUninstallingPlugin}
            onPress={this.onUninstallPluginPress}
          />
        </TableRowCell>
      </TableRow>
    );
  }
}

PluginRow.propTypes = {
  githubUrl: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  owner: PropTypes.string.isRequired,
  installedVersion: PropTypes.string.isRequired,
  availableVersion: PropTypes.string.isRequired,
  updateAvailable: PropTypes.bool.isRequired,
  isInstallingPlugin: PropTypes.bool.isRequired,
  onInstallPluginPress: PropTypes.func.isRequired,
  isUninstallingPlugin: PropTypes.bool.isRequired,
  onUninstallPluginPress: PropTypes.func.isRequired
};

export default PluginRow;
