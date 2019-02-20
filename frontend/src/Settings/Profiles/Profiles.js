import React, { Component } from 'react';
import { DragDropContext } from 'react-dnd';
import HTML5Backend from 'react-dnd-html5-backend';
import PageContent from 'Components/Page/PageContent';
import PageContentBodyConnector from 'Components/Page/PageContentBodyConnector';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import QualityProfilesConnector from './Quality/QualityProfilesConnector';
import LanguageProfilesConnector from './Language/LanguageProfilesConnector';
import MetadataProfilesConnector from './Metadata/MetadataProfilesConnector';
import DelayProfilesConnector from './Delay/DelayProfilesConnector';
import ReleaseProfilesConnector from './Release/ReleaseProfilesConnector';

class Profiles extends Component {

  //
  // Render

  render() {
    return (
      <PageContent title="Profiles">
        <SettingsToolbarConnector
          showSave={false}
        />

        <PageContentBodyConnector>
          <QualityProfilesConnector />
          <LanguageProfilesConnector />
          <MetadataProfilesConnector />
          <DelayProfilesConnector />
          <ReleaseProfilesConnector />
        </PageContentBodyConnector>
      </PageContent>
    );
  }
}

// Only a single DragDropContext can exist so it's done here to allow editing
// quality profiles and reordering delay profiles to work.
/* eslint-disable new-cap */
export default DragDropContext(HTML5Backend)(Profiles);
/* eslint-enable new-cap */
