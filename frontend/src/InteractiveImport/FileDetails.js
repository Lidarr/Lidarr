import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { icons } from 'Helpers/Props';
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
                    title="Artist"
                    data={tags.artistTitle}
                  />
                  <DescriptionListItem
                    title="Album"
                    data={tags.albumTitle}
                  />
                  <DescriptionListItem
                    title="Disc Number"
                    data={tags.discNumber}
                  />
                  <DescriptionListItem
                    title="Track Number"
                    data={tags.trackNumbers[0]}
                  />
                  <DescriptionListItem
                    title="Track Title"
                    data={tags.title}
                  />
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
