import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { icons } from 'Helpers/Props';
import formatTimeSpan from 'Utilities/Date/formatTimeSpan';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import styles from './FileDetails.css';

class FileDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isExpanded: props.isExpanded
    };
  }

  //
  // Listeners

  onExpandPress = () => {
    const {
      isExpanded
    } = this.state;
    this.setState({ isExpanded: !isExpanded });
  }

  //
  // Render

  renderRejections() {
    const {
      rejections
    } = this.props;

    return (
      <span>
        <DescriptionListItemTitle>
          Rejections
        </DescriptionListItemTitle>
        {
          _.map(rejections, (item, key) => {
            return (
              <DescriptionListItemDescription key={key}>
                {item.reason}
              </DescriptionListItemDescription>
            );
          })
        }
      </span>
    );
  }

  render() {
    const {
      filename,
      tags,
      rejections
    } = this.props;

    const {
      isExpanded
    } = this.state;

    return (
      <div
        className={styles.fileDetails}
      >
        <div className={styles.header} onClick={this.onExpandPress}>
          <div className={styles.filename}>
            {filename}
          </div>

          <div className={styles.expandButton}>
            <Icon
              className={styles.expandButtonIcon}
              name={isExpanded ? icons.COLLAPSE : icons.EXPAND}
              title={isExpanded ? 'Hide file info' : 'Show file info'}
              size={24}
            />
          </div>
        </div>

        <div>
          {
            isExpanded &&
              <div className={styles.tags}>

                <DescriptionList>
                  <DescriptionListItem
                    title="Track Title"
                    data={tags.title}
                  />
                  <DescriptionListItem
                    title="Track Number"
                    data={tags.trackNumbers[0]}
                  />
                  <DescriptionListItem
                    title="Disc Number"
                    data={tags.discNumber}
                  />
                  {
                    tags.discCount !== undefined && tags.discCount > 0 &&
                    <DescriptionListItem
                      title="Disc Count"
                      data={tags.discCount}
                    />
                  }
                  <DescriptionListItem
                    title="Album"
                    data={tags.albumTitle}
                  />
                  <DescriptionListItem
                    title="Artist"
                    data={tags.artistTitle}
                  />
                  {
                    tags.country !== undefined &&
                    <DescriptionListItem
                      title="Country"
                      data={tags.country.name}
                    />
                  }
                  {
                    tags.year !== undefined &&
                    <DescriptionListItem
                      title="Year"
                      data={tags.year}
                    />
                  }
                  {
                    tags.label !== undefined &&
                    <DescriptionListItem
                      title="Label"
                      data={tags.label}
                    />
                  }
                  {
                    tags.catalogNumber !== undefined &&
                    <DescriptionListItem
                      title="Catalog Number"
                      data={tags.catalogNumber}
                    />
                  }
                  {
                    tags.disambiguation !== undefined &&
                    <DescriptionListItem
                      title="Disambiguation"
                      data={tags.disambiguation}
                    />
                  }
                  {
                    tags.duration !== undefined &&
                    <DescriptionListItem
                      title="Duration"
                      data={formatTimeSpan(tags.duration)}
                    />
                  }
                  {
                    tags.artistMBId !== undefined &&
                    <Link
                      to={`https://musicbrainz.org/artist/${tags.artistMBId}`}
                    >
                      <DescriptionListItem
                        title="MusicBrainz Artist ID"
                        data={tags.artistMBId}
                      />
                    </Link>
                  }
                  {
                    tags.albumMBId !== undefined &&
                    <Link
                      to={`https://musicbrainz.org/release-group/${tags.albumMBId}`}
                    >
                      <DescriptionListItem
                        title="MusicBrainz Album ID"
                        data={tags.albumMBId}
                      />
                    </Link>
                  }
                  {
                    tags.releaseMBId !== undefined &&
                    <Link
                      to={`https://musicbrainz.org/release/${tags.releaseMBId}`}
                    >
                      <DescriptionListItem
                        title="MusicBrainz Release ID"
                        data={tags.releaseMBId}
                      />
                    </Link>
                  }
                  {
                    tags.recordingMBId !== undefined &&
                    <Link
                      to={`https://musicbrainz.org/recording/${tags.recordingMBId}`}
                    >
                      <DescriptionListItem
                        title="MusicBrainz Recording ID"
                        data={tags.recordingMBId}
                      />
                    </Link>
                  }
                  {
                    tags.trackMBId !== undefined &&
                    <Link
                      to={`https://musicbrainz.org/track/${tags.trackMBId}`}
                    >
                      <DescriptionListItem
                        title="MusicBrainz Track ID"
                        data={tags.trackMBId}
                      />
                    </Link>
                  }
                  {rejections.length > 0 && this.renderRejections()}
                </DescriptionList>
              </div>
          }
        </div>
      </div>
    );
  }
}

FileDetails.propTypes = {
  tags: PropTypes.object.isRequired,
  filename: PropTypes.string.isRequired,
  rejections: PropTypes.arrayOf(PropTypes.object).isRequired,
  isExpanded: PropTypes.bool
};

export default FileDetails;
