import classNames from 'classnames';
import PropTypes from 'prop-types';
import React from 'react';
import formatBytes from 'Utilities/Number/formatBytes';
import EnhancedSelectInputOption from './EnhancedSelectInputOption';
import styles from './RootFolderSelectInputOption.css';

function RootFolderSelectInputOption(props) {
  const {
    id,
    value,
    name,
    freeSpace,
    artistFolder,
    isMobile,
    isWindows,
    ...otherProps
  } = props;

  const slashCharacter = isWindows ? '\\' : '/';

  const text = value === '' ? name : `[${name}] ${value}`;

  console.debug(props);

  return (
    <EnhancedSelectInputOption
      id={id}
      isMobile={isMobile}
      {...otherProps}
    >
      <div className={classNames(
        styles.optionText,
        isMobile && styles.isMobile
      )}
      >
        <div className={styles.value}>
          {text}

          {
            artistFolder && id !== 'addNew' ?
              <div className={styles.artistFolder}>
                {slashCharacter}
                {artistFolder}
              </div> :
              null
          }
        </div>

        {
          freeSpace != null &&
            <div className={styles.freeSpace}>
              {formatBytes(freeSpace)} Free
            </div>
        }
      </div>
    </EnhancedSelectInputOption>
  );
}

RootFolderSelectInputOption.propTypes = {
  id: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  freeSpace: PropTypes.number,
  artistFolder: PropTypes.string,
  isMobile: PropTypes.bool.isRequired,
  isWindows: PropTypes.bool
};

export default RootFolderSelectInputOption;
