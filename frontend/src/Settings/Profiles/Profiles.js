import React, { Component } from 'react';
import { DndProvider } from 'react-dnd';
import HTML5Backend from 'react-dnd-html5-backend';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import DelayProfilesConnector from './Delay/DelayProfilesConnector';
import MetadataProfilesConnector from './Metadata/MetadataProfilesConnector';
import QualityProfilesConnector from './Quality/QualityProfilesConnector';
import ReleaseProfilesConnector from './Release/ReleaseProfilesConnector';

// Only a single DragDrop Context can exist so it's done here to allow editing
// quality profiles and reordering delay profiles to work.

class Profiles extends Component {

  //
  // Render

  render() {
    return (
      <PageContent title="Profiles">
        <SettingsToolbarConnector
          showSave={false}
        />

        <PageContentBody>
          <DndProvider backend={HTML5Backend}>
            <QualityProfilesConnector />
            <MetadataProfilesConnector />
            <DelayProfilesConnector />
            <ReleaseProfilesConnector />
          </DndProvider>
        </PageContentBody>
      </PageContent>
    );
  }
}

export default Profiles;
