import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchGeneralSettings, saveGeneralSettings, setGeneralSettingsValue } from 'Store/Actions/settingsActions';
import { clearTaskPending, fetchTasks, saveTasks, setTaskInterval, setTaskPending } from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import GeneralSettings from './GeneralSettings';

const SECTION = 'general';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    (state) => state.system.tasks,
    createSettingsSectionSelector(SECTION),
    createCommandExecutingSelector(commandNames.RESET_API_KEY),
    createSystemStatusSelector(),
    (advancedSettings, tasks, sectionSettings, isResettingApiKey, systemStatus) => {
      const hasTaskPendingChanges = Object.keys(tasks.pendingChanges || {}).length > 0;
      const hasPendingChanges = hasTaskPendingChanges || sectionSettings.hasPendingChanges;

      return {
        advancedSettings,
        tasks,
        isResettingApiKey,
        isWindows: systemStatus.isWindows,
        isWindowsService: systemStatus.isWindows && systemStatus.mode === 'service',
        mode: systemStatus.mode,
        packageUpdateMechanism: systemStatus.packageUpdateMechanism,
        ...sectionSettings,
        hasPendingChanges
      };
    }
  );
}

const mapDispatchToProps = {
  setGeneralSettingsValue,
  saveGeneralSettings,
  fetchGeneralSettings,
  fetchTasks,
  setTaskInterval,
  setTaskPending,
  saveTasks,
  clearTaskPending,
  executeCommand,
  clearPendingChanges
};

class GeneralSettingsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchGeneralSettings();
    this.props.clearTaskPending();
    this.props.fetchTasks();
  }

  componentDidUpdate(prevProps) {
    if (!this.props.isResettingApiKey && prevProps.isResettingApiKey) {
      this.props.fetchGeneralSettings();
    }
  }

  componentWillUnmount() {
    this.props.clearPendingChanges({ section: `settings.${SECTION}` });
    this.props.clearTaskPending();
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setGeneralSettingsValue({ name, value });
  };

  onSavePress = () => {
    this.props.saveTasks();
    this.props.saveGeneralSettings();
  };

  onConfirmResetApiKey = () => {
    this.props.executeCommand({ name: commandNames.RESET_API_KEY });
  };

  onConfirmRestart = () => {
    this.props.restart();
  };

  //
  // Render

  render() {
    return (
      <GeneralSettings
        onInputChange={this.onInputChange}
        onSavePress={this.onSavePress}
        onConfirmResetApiKey={this.onConfirmResetApiKey}
        onConfirmRestart={this.onConfirmRestart}
        {...this.props}
      />
    );
  }
}

GeneralSettingsConnector.propTypes = {
  isResettingApiKey: PropTypes.bool.isRequired,
  setGeneralSettingsValue: PropTypes.func.isRequired,
  saveGeneralSettings: PropTypes.func.isRequired,
  fetchGeneralSettings: PropTypes.func.isRequired,
  fetchTasks: PropTypes.func.isRequired,
  setTaskInterval: PropTypes.func.isRequired,
  setTaskPending: PropTypes.func.isRequired,
  saveTasks: PropTypes.func.isRequired,
  clearTaskPending: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired,
  restart: PropTypes.func.isRequired,
  clearPendingChanges: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(GeneralSettingsConnector);
