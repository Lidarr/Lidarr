import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { inputTypes, kinds } from 'Helpers/Props';

class Plugins extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      repoUrl: null
    };
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.setState({
      [name]: value
    });
  }

  onInstallPluginPress = () => {
    this.props.onInstallPluginPress(this.state.repoUrl);
  }

  //
  // Render

  render() {
    const {
      isInstallingPlugin
    } = this.props;

    const {
      repoUrl
    } = this.state;

    return (
      <PageContent title="Plugins">
        <PageContentBody>
          <Form>
            <FieldSet legend="Install Plugin">
              <FormGroup>
                <FormLabel>GitHub URL</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="repoUrl"
                  helpText="URL to GitHub repository containing plugin"
                  helpLink="https://wiki.servarr.com/Lidarr_FAQ#How_do_I_install_plugins"
                  value={repoUrl}
                  onChange={this.onInputChange}
                />
              </FormGroup>
              <SpinnerButton
                kind={kinds.PRIMARY}
                isSpinning={isInstallingPlugin}
                onPress={this.onInstallPluginPress}
              >
                Install Plugin
              </SpinnerButton>
            </FieldSet>
          </Form>
        </PageContentBody>
      </PageContent>
    );
  }
}

Plugins.propTypes = {
  isInstallingPlugin: PropTypes.bool.isRequired,
  onInstallPluginPress: PropTypes.func.isRequired
};

export default Plugins;
