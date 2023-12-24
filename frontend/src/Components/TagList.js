import PropTypes from 'prop-types';
import React from 'react';
import { kinds } from 'Helpers/Props';
import Label from './Label';
import styles from './TagList.css';

function TagList({ className, tags, tagList }) {
  const sortedTags = tags
    .map((tagId) => tagList.find((tag) => tag.id === tagId))
    .filter((tag) => !!tag)
    .sort((a, b) => a.label.localeCompare(b.label));

  return (
    <div className={className}>
      {
        sortedTags.map((tag) => {
          return (
            <Label
              key={tag.id}
              kind={kinds.INFO}
            >
              {tag.label}
            </Label>
          );
        })
      }
    </div>
  );
}

TagList.propTypes = {
  className: PropTypes.string.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  tagList: PropTypes.arrayOf(PropTypes.object).isRequired
};

TagList.defaultProps = {
  className: styles.tags
};

export default TagList;
