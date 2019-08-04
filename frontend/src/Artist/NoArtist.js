import PropTypes from 'prop-types';
import React from 'react';
import Button from 'Components/Link/Button';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NoArtist.css';

function NoArtist(props) {
  const { totalItems } = props;

  if (totalItems > 0) {
    return (
      <div>
        <div className={styles.message}>
          All artists are hidden due to the applied filter.
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.message}>
        No artists found, to get started you'll want to add a new artist or album or add an existing library location (Root Folder) and update.
      </div>

      <div className={styles.buttonContainer}>
        <Button
          to="/settings/mediamanagement"
          kind={kinds.PRIMARY}
        >
          {translate('AddRootFolder')}
        </Button>
      </div>

      <div className={styles.buttonContainer}>
        <Button
          to="/add/search"
          kind={kinds.PRIMARY}
        >
          {translate('AddNewArtist')}
        </Button>
      </div>
    </div>
  );
}

NoArtist.propTypes = {
  totalItems: PropTypes.number.isRequired
};

export default NoArtist;
