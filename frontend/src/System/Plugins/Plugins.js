import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import PluginRow from './PluginRow';

const columns = [
  {
    name: 'name',
    label: 'Name',
    isVisible: true
  },
  {
    name: 'owner',
    label: 'Owner',
    isVisible: true
  },
  {
    name: 'installedVersion',
    label: 'Installed Version',
    isVisible: true
  },
  {
    name: 'availableVersion',
    label: 'Available Version',
    isVisible: true
  },
  {
    name: 'actions',
    isVisible: true
  }
];

class Plugins extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      repoUrl: ''
    };
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.setState({
      [name]: value
    });
  };

  onInstallPluginPress = () => {
    this.props.onInstallPluginPress(this.state.repoUrl);
  };

  //
  // Render

  render() {
    const {
      isPopulated,
      error,
      items,
      isInstallingPlugin,
      onInstallPluginPress,
      isUninstallingPlugin,
      onUninstallPluginPress,
      isRestartRequiredModalOpen,
      onCloseRestartRequiredModal,
      pluginOwner,
      pluginName,
      pluginVersion,
      pluginAction,
      pluginDetailsUrl,
      pluginBranch
    } = this.props;

    const {
      repoUrl
    } = this.state;

    const noPlugins = isPopulated && !error && !items.length;

    // Build modal title and message
    let modalTitle = translate('RestartRequired');
    let modalMessage = translate('LidarrRequiresRestartToApplyPluginChanges');

    if (pluginOwner && pluginName && pluginAction) {
      const versionText = pluginVersion ? ` v${pluginVersion}` : '';
      const branchText = pluginBranch ? ` (${pluginBranch})` : '';
      const actionText = pluginAction === 'install' ? 'installed' : 'uninstalled';
      modalTitle = `Plugin ${actionText} - ${translate('RestartRequired')}`;

      if (pluginDetailsUrl) {
        modalMessage = `Plugin: [${pluginOwner}/${pluginName}]${versionText}${branchText}\n\nInstalled from:\n${pluginDetailsUrl}\n\nPlease restart Lidarr to apply changes.`;
      } else {
        modalMessage = `Plugin: [${pluginOwner}/${pluginName}]${versionText}${branchText}\n\nPlugin ${actionText} successfully.\n\nPlease restart Lidarr to apply changes.`;
      }
    }

    return (
      <PageContent title="Plugins">
        <PageContentBody>
          <Form>
            <FieldSet legend="Install New Plugin">
              <FormGroup>
                <FormLabel>GitHub URL</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="repoUrl"
                  helpText="URL to GitHub repository containing plugin"
                  helpLink="https://wiki.servarr.com/lidarr/plugins"
                  value={repoUrl}
                  onChange={this.onInputChange}
                />
              </FormGroup>
              <SpinnerButton
                kind={kinds.PRIMARY}
                isSpinning={isInstallingPlugin}
                onPress={this.onInstallPluginPress}
              >
                Install
              </SpinnerButton>
            </FieldSet>
          </Form>
          <FieldSet legend="Installed Plugins">
            {
              !isPopulated && !error &&
                <LoadingIndicator />
            }

            {
              isPopulated && noPlugins &&
                <div>No plugins are installed</div>
            }

            {
              isPopulated && !noPlugins &&
                <Table
                  columns={columns}
                >
                  <TableBody>
                    {
                      items.map((plugin) => {
                        return (
                          <PluginRow
                            key={plugin.githubUrl}
                            {...plugin}
                            isInstallingPlugin={isInstallingPlugin}
                            isUninstallingPlugin={isUninstallingPlugin}
                            onInstallPluginPress={onInstallPluginPress}
                            onUninstallPluginPress={onUninstallPluginPress}
                          />
                        );
                      })
                    }
                  </TableBody>
                </Table>
            }
          </FieldSet>

          <ConfirmModal
            isOpen={isRestartRequiredModalOpen}
            kind={kinds.INFO}
            title={modalTitle}
            message={modalMessage}
            confirmLabel={translate('Ok')}
            hideCancelButton={true}
            onConfirm={onCloseRestartRequiredModal}
            onCancel={onCloseRestartRequiredModal}
          />
        </PageContentBody>
      </PageContent>
    );
  }
}

Plugins.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  items: PropTypes.array.isRequired,
  isInstallingPlugin: PropTypes.bool.isRequired,
  onInstallPluginPress: PropTypes.func.isRequired,
  isUninstallingPlugin: PropTypes.bool.isRequired,
  onUninstallPluginPress: PropTypes.func.isRequired,
  isRestartRequiredModalOpen: PropTypes.bool,
  onCloseRestartRequiredModal: PropTypes.func,
  pluginOwner: PropTypes.string,
  pluginName: PropTypes.string,
  pluginVersion: PropTypes.string,
  pluginAction: PropTypes.string,
  pluginDetailsUrl: PropTypes.string,
  pluginBranch: PropTypes.string
};

export default Plugins;
