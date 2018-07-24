import PropTypes from 'prop-types';
import React, { Component } from 'react';
import titleCase from 'Utilities/String/titleCase';
import FieldSet from 'Components/FieldSet';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import styles from './About.css';

class About extends Component {

  //
  // Render

  render() {
    const {
      version,
      isMonoRuntime,
      runtimeVersion,
      migrationVersion,
      appData,
      startupPath,
      mode
    } = this.props;

    return (
      <FieldSet legend="About">
        <DescriptionList className={styles.descriptionList}>
          <DescriptionListItem
            title="Version"
            data={version}
          />

          {
            isMonoRuntime &&
              <DescriptionListItem
                title="Mono Version"
                data={runtimeVersion}
              />
          }

          <DescriptionListItem
            title="DB Migration"
            data={migrationVersion}
          />

          <DescriptionListItem
            title="AppData directory"
            data={appData}
          />

          <DescriptionListItem
            title="Startup directory"
            data={startupPath}
          />

          <DescriptionListItem
            title="Mode"
            data={titleCase(mode)}
          />
        </DescriptionList>
      </FieldSet>
    );
  }

}

About.propTypes = {
  version: PropTypes.string,
  isMonoRuntime: PropTypes.bool,
  runtimeVersion: PropTypes.string,
  migrationVersion: PropTypes.number,
  appData: PropTypes.string,
  startupPath: PropTypes.string,
  mode: PropTypes.string
};

export default About;
