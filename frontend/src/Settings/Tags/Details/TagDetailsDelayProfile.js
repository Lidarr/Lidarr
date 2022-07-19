import PropTypes from 'prop-types';
import React from 'react';
import DelayProfileItem from 'Settings/Profiles/Delay/DelayProfileItem';
import styles from './TagDetailsDelayProfile.css';

function TagDetailsDelayProfile(props) {
  const {
    name: profileName,
    items
  } = props;

  return (
    <div
      className={styles.delayProfile}
    >
      <div
        className={styles.name}
      >
        {profileName}
      </div>

      <div className={styles.tags}>
        {
          items.map((item) => {
            const {
              protocol,
              name,
              allowed
            } = item;

            return (
              <DelayProfileItem
                key={protocol}
                name={name}
                allowed={allowed}
              />
            );
          })
        }
      </div>
    </div>
  );
}

TagDetailsDelayProfile.propTypes = {
  name: PropTypes.string.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default TagDetailsDelayProfile;
