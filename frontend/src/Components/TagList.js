import _ from 'lodash';
import PropTypes from 'prop-types';
import React from 'react';
import { kinds } from 'Helpers/Props';
import Label from './Label';
import styles from './TagList.css';

function TagList({ className, tags, tagList }) {
  return (
    <div className={className}>
      {
        tags.map((t) => {
          const tag = _.find(tagList, { id: t });

          if (!tag) {
            return null;
          }

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
