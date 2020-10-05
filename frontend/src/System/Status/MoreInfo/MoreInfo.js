import React, { Component } from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';

class MoreInfo extends Component {

  //
  // Render

  render() {
    return (
      <FieldSet legend="More Info">
        <DescriptionList>
          <DescriptionListItemTitle>Home page</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://lidarr.audio/">lidarr.audio</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Wiki</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://github.com/lidarr/Lidarr/wiki">wiki.lidarr.audio</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Reddit</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://www.reddit.com/r/Lidarr/">Lidarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Discord</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://discord.gg/5WQc3S8">#lidarr on Discord</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Donations</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://opencollective.com/lidarr">Donate to Lidarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Sonarr Donations</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://sonarr.tv/donate">Donate to Sonarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Source</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://github.com/lidarr/Lidarr/">github.com/Lidarr/Lidarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Feature Requests</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://github.com/lidarr/Lidarr/issues">github.com/Lidarr/Lidarr/issues</Link>
          </DescriptionListItemDescription>

        </DescriptionList>
      </FieldSet>
    );
  }
}

MoreInfo.propTypes = {

};

export default MoreInfo;
